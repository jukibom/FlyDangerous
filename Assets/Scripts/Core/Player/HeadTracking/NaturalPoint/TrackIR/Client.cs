//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;


namespace NaturalPoint.TrackIR
{
    public class Client
    {
        /// <summary>
        /// The "major" component of the TrackIR software version number.
        /// </summary>
        public int HostSoftwareVersionMajor { get; private set; }

        /// <summary>
        /// The "minor" component of the TrackIR software version number.
        /// </summary>
        public int HostSoftwareVersionMinor { get; private set; }

        /// <summary>
        /// The most recent head tracking pose data. Updated by <see cref="UpdatePose"/>.
        /// </summary>
        public Pose LatestPose { get { return m_latestPose; } }

        /// <summary>
        /// Backing field for the <see cref="LatestPose"/> property.
        /// </summary>
        Pose m_latestPose;

        /// <summary>
        /// NaturalPoint-assigned application ID that was passed to the constructor.
        /// </summary>
        UInt16 m_appId;

        /// <summary>
        /// Application foreground window handle that was passed to the constructor.
        /// </summary>
        IntPtr m_appHwnd;

        /// <summary>
        /// Helper class exposing dynamic library function pointer delegates.
        /// </summary>
        NativeClientLibrary m_clientLib;

        /// <summary>
        /// Cached, native data structure returned by <see cref="NativeClientLibrary.NP_GetData"/>.
        /// </summary>
        TrackIRData m_trackirData;

        /// <summary>
        /// Value of <see cref="TrackIRData.FrameSignature"/> from data corresponding to <see cref="LatestPose"/>.
        /// </summary>
        Int32 m_latestFrameSignature;

        /// <summary>
        /// If true, NP_RegisterWindowHandle succeeded, and we need to call NP_UnregisterWindowHandle correspondingly.
        /// </summary>
        bool m_bRegisteredWindowHandle;

        /// <summary>
        /// If true, NP_StartDataTransmission succeeded, and we need to call NP_StopDataTransmission correspondingly.
        /// </summary>
        bool m_bDataTransmitting;

        /// <summary>
        /// If true, we've either connected and then subsequently lost connection, or we're retrying the initial
        /// connection attempt, because we received ERR_DEVICE_NOT_PRESENT and are waiting for the device or
        /// software to become available again.
        /// </summary>
        bool m_bPendingReconnect;


        /// <summary>
        /// Loads the TrackIR Enhanced dynamic library and initializes the API.
        /// </summary>
        /// <param name="applicationId">Your NaturalPoint-assigned application ID.</param>
        /// <param name="hwnd">The handle for the foreground window of your application.</param>
        public Client( UInt16 applicationId, IntPtr hwnd )
        {
            if ( applicationId == 0 )
            {
                throw new ArgumentException( "Application ID cannot be zero." );
            }

            if ( hwnd == IntPtr.Zero )
            {
                throw new ArgumentException( "hwnd cannot be zero." );
            }

            m_appId = applicationId;
            m_appHwnd = hwnd;

            // This can throw if the TrackIR Enhanced client library wasn't loaded successfully.
            m_clientLib = new NativeClientLibrary();

            // FrameSignature value from TrackIR Enhanced API is unsigned, so we use a negative initial value to
            // guarantee inequality compared to the first value from the API.
            m_latestFrameSignature = -1;

            m_latestPose = Pose.Identity;

            InternalConnect();
        }


        ~Client()
        {
            Disconnect();
        }


        /// <summary>
        /// Stops data transmission and unregisters the application's window handle.
        /// </summary>
        public void Disconnect()
        {
            if ( m_bDataTransmitting )
            {
                NPResult stopDataResult = m_clientLib.NP_StopDataTransmission();
                if ( stopDataResult != NPResult.OK )
                {
                    Console.WriteLine( "WARNING: NP_StopDataTransmission returned " + stopDataResult.ToString() + "." );
                }

                m_bDataTransmitting = false;
            }

            if ( m_bRegisteredWindowHandle )
            {
                NPResult unregisterResult = m_clientLib.NP_UnregisterWindowHandle();
                if ( unregisterResult != NPResult.OK )
                {
                    Console.WriteLine( "WARNING: NP_UnregisterWindowHandle returned " + unregisterResult.ToString() + "." );
                }

                m_bRegisteredWindowHandle = false;
            }
        }


        /// <summary>
        /// Reinitializes the API following a disconnect.
        /// </summary>
        public void Reconnect()
        {
            // This is a no-op if we aren't currently "connected."
            Disconnect();

            InternalConnect();
        }


        /// <summary>
        /// If new data is available, updates <see cref="LatestPose"/>.
        /// </summary>
        /// <remarks>
        /// If the client "connection" was interrupted, this call will automatically attempt to reconnect. If
        /// something goes wrong during that process, it's possible that the call could throw an exception.
        /// </remarks>
        /// <returns>True if new data was available, false otherwise.</returns>
        public bool UpdatePose()
        {
            if ( m_bPendingReconnect )
            {
                // Note: This call could throw.
                if ( InternalConnect() )
                {
                    // Successfully reconnected; clear the flag and continue normally.
                    m_bPendingReconnect = false;
                }
                else
                {
                    // Failed to reconnect; don't go any further, but try to reconnect again next time.
                    return false;
                }
            }

            NPResult getDataResult = m_clientLib.NP_GetData( ref m_trackirData, 0, 0 );

            if ( getDataResult == NPResult.OK )
            {
                if ( m_trackirData.FrameSignature != m_latestFrameSignature )
                {
                    // We got fresh data, so update the latest cached/transformed pose.
                    const float kEncodedRangeMinMax = 16383.0f;
                    const float kDecodedTranslationMinMaxMeters = 0.5f; // +/- 50 cm
                    const float kDecodedRotationMinMaxRadians = (float)Math.PI; // +/- 180 deg

                    // Negate right-hand rule rotations to be consistent with left-handed coordinate basis.
                    // See remarks in the comments for the TrackIRData struct for more information.
                    float rollRad = (-m_trackirData.Roll / kEncodedRangeMinMax) * kDecodedRotationMinMaxRadians;
                    float pitchRad = (-m_trackirData.Pitch / kEncodedRangeMinMax) * kDecodedRotationMinMaxRadians;
                    float yawRad = (-m_trackirData.Yaw / kEncodedRangeMinMax) * kDecodedRotationMinMaxRadians;
                    float xMeters = (m_trackirData.X / kEncodedRangeMinMax) * kDecodedTranslationMinMaxMeters;
                    float yMeters = (m_trackirData.Y / kEncodedRangeMinMax) * kDecodedTranslationMinMaxMeters;
                    float zMeters = (m_trackirData.Z / kEncodedRangeMinMax) * kDecodedTranslationMinMaxMeters;

                    m_latestPose.Orientation = Pose.Quaternion.FromTaitBryanIntrinsicZYX( rollRad, yawRad, pitchRad );
                    m_latestPose.PositionMeters = new Pose.Vector3( xMeters, yMeters, zMeters );

                    m_latestFrameSignature = m_trackirData.FrameSignature;

                    // Successfully retrieved a new pose.
                    return true;
                }
                else
                {
                    // No error, but no new data available.
                    return false;
                }
            }
            else
            {
                // Got an unexpected return code from the NP_GetData call.
                m_latestFrameSignature = -1;
                m_bPendingReconnect = true;
                return false;
            }
        }


        /// <summary>
        /// Uses the cached appId and hwnd to initialize the TrackIR API.
        /// </summary>
        /// <returns>
        /// True if initialization completed successfully, false if the API returned the code
        /// <see cref="NPResult.ERR_DEVICE_NOT_PRESENT"/>. Any other failure condition throws an exception.
        /// </returns>
        private bool InternalConnect()
        {
            // Clear this flag. Most exit paths from this function represent errors we can't recover from, and we
            // shouldn't keep trying repeatedly; the only exception is waiting for ERR_DEVICE_NOT_PRESENT to resolve.
            m_bPendingReconnect = false;

            // Retrieve host software version.
            UInt16 version;
            NPResult queryVersionResult = m_clientLib.NP_QueryVersion( out version );
            if ( queryVersionResult == NPResult.ERR_DEVICE_NOT_PRESENT )
            {
                // Don't go any further; we'll try to connect again next time UpdatePose is called.
                m_bPendingReconnect = true;
                return false;
            }
            else if ( queryVersionResult != NPResult.OK )
            {
                Console.WriteLine( "WARNING: NP_QueryVersion returned " + queryVersionResult.ToString() + "." );
            }

            HostSoftwareVersionMajor = (version >> 8);
            HostSoftwareVersionMinor = (version & 0x00FF);

            // Retrieve signature object and validate that signature strings match expected values.
            TrackIRSignature signature = new TrackIRSignature();
            NPResult getSignatureResult = m_clientLib.NP_GetSignature( ref signature );

            if ( getSignatureResult != NPResult.OK )
            {
                throw new TrackIRException( "NP_GetSignature returned " + getSignatureResult.ToString() + "." );
            }

            const string kExpectedAppSignature = "hardware camera\n software processing data\n track user movement\n\n Copyright EyeControl Technologies";
            const string kExpectedDllSignature = "precise head tracking\n put your head into the game\n now go look around\n\n Copyright EyeControl Technologies";

            if ( signature.AppSignature != kExpectedAppSignature || signature.DllSignature != kExpectedDllSignature )
            {
                throw new TrackIRException( "Unable to verify TrackIR Enhanced signature values." );
            }

            // Register the application window for liveness checks. This allows the TrackIR software to
            // detect situations where e.g. your application crashes and fails to shut down cleanly.
            NPResult registerHwndResult = m_clientLib.NP_RegisterWindowHandle( m_appHwnd );
            if ( registerHwndResult == NPResult.OK )
            {
                m_bRegisteredWindowHandle = true;
            }
            else
            {
                throw new TrackIRException( "NP_RegisterWindowHandle returned " + registerHwndResult.ToString() + "." );
            }

            // Register the application by its NaturalPoint-assigned ID.
            NPResult registerIdResult = m_clientLib.NP_RegisterProgramProfileID( m_appId );
            if ( registerIdResult != NPResult.OK )
            {
                throw new TrackIRException( "NP_RegisterProgramProfileID returned " + registerIdResult.ToString() + "." );
            }

            // Signal that we want to start receiving tracking data.
            NPResult startDataResult = m_clientLib.NP_StartDataTransmission();
            if ( startDataResult == NPResult.OK )
            {
                m_bDataTransmitting = true;
            }
            else
            {
                throw new TrackIRException( "NP_StartDataTransmission returned " + startDataResult.ToString() + "." );
            }

            return true;
        }
    }
} // namespace NaturalPoint.TrackIR
