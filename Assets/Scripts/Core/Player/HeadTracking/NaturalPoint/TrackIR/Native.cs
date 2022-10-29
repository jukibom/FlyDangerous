//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace NaturalPoint.TrackIR
{
    /// <summary>
    /// Return codes used by TrackIR C API functions.
    /// </summary>
    public enum NPResult
    {
        OK = 0,
        ERR_DEVICE_NOT_PRESENT,
        ERR_UNSUPPORTED_OS,
        ERR_INVALID_ARG,
        ERR_DLL_NOT_FOUND,
        ERR_NO_DATA,
        ERR_INTERNAL_DATA,
        ERR_ALREADY_REGISTERED,
        ERR_UNKNOWN_ID,

        ERR_FAILED = 100,
        ERR_INVALID_KEY
    }


    /// <summary>
    /// This structure represents a single frame of head tracking pose data returned by the NP_GetData function.
    /// </summary>
    /// <remarks>
    /// For historical reasons, the TrackIR Enhanced API uses a non-standard, "hybrid" coordinate system, with
    /// left-handed basis vectors - from the tracked user's perspective, facing the display + TrackIR device:
    ///     +X = Left
    ///     +Y = Up
    ///     +Z = Back
    ///
    /// And right-hand rule rotations about those vectors:
    ///     Pitch = Rotation about X, increasing as the user looks down.
    ///     Yaw = Rotation about Y, increasing as the user turns left.
    ///     Roll = Rotation about Z, increasing as the user tilts left.
    ///
    /// The X, Y, and Z members define the position. The values range from -16383.0f to 16383.0f, and map to a range
    /// of -50 cm to +50 cm.
    ///
    /// The Roll, Pitch, and Yaw members represent orientation as an intrinsic rotation using Tait–Bryan angles.
    /// To be consistent with the TrackIR 5 software viewport, they should be applied in the order z-y'-x'' (R-Y-P).
    /// The values range from -16383.0f to 16383.0f, and map to a range of -180 degrees to +180 degrees.
    /// </remarks>
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct TrackIRData
    {
        public UInt16 Status;
        public UInt16 FrameSignature;
        public UInt32 IOData;

        public float Roll;
        public float Pitch;
        public float Yaw;
        public float X;
        public float Y;
        public float Z;

        // This approach renders the struct non-blittable and leads to GC alloc during marshalling...
//      [MarshalAs( UnmanagedType.ByValArray, ArraySubType = UnmanagedType.R4, SizeConst = 9 )]
//      private float[] Reserved;

        // ...whereas this more verbose approach avoids generating any garbage.
        private float Reserved1;
        private float Reserved2;
        private float Reserved3;
        private float Reserved4;
        private float Reserved5;
        private float Reserved6;
        private float Reserved7;
        private float Reserved8;
        private float Reserved9;
    }


    /// <summary>
    /// This structure contains strings set by both the TrackIR Enhanced DLL and the TrackIR application itself. Used
    /// for integrity checking.
    /// </summary>
    [StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi )]
    public struct TrackIRSignature
    {
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 200 )]
        public string DllSignature;

        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 200 )]
        public string AppSignature;
    }


    /// <summary>
    /// Thin interop wrapper for native TrackIR C API functions.
    /// </summary>
    /// <remarks>
    /// DllImport is a bad fit for this because we need an explicit DLL path (which is read from a registry key set by
    /// the TrackIR software). Instead, we expose UnmanagedFunctionPointer delegates on the instance.
    /// </remarks>
    public class NativeClientLibrary
    {
        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_RegisterWindowHandle_Delegate( IntPtr hwnd );

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_UnregisterWindowHandle_Delegate();

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_RegisterProgramProfileID_Delegate( UInt16 ppid );

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_QueryVersion_Delegate( out UInt16 version );

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_GetSignature_Delegate( ref TrackIRSignature signature );

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_GetData_Delegate( ref TrackIRData data, UInt32 appKeyHigh, UInt32 appKeyLow );

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_ReCenter_Delegate();

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_StartDataTransmission_Delegate();

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        public delegate NPResult NP_StopDataTransmission_Delegate();


        public NP_RegisterWindowHandle_Delegate NP_RegisterWindowHandle { get; private set; }
        public NP_UnregisterWindowHandle_Delegate NP_UnregisterWindowHandle { get; private set; }
        public NP_RegisterProgramProfileID_Delegate NP_RegisterProgramProfileID { get; private set; }
        public NP_QueryVersion_Delegate NP_QueryVersion { get; private set; }
        public NP_GetSignature_Delegate NP_GetSignature { get; private set; }
        public NP_GetData_Delegate NP_GetData { get; private set; }
        public NP_ReCenter_Delegate NP_ReCenter { get; private set; }
        public NP_StartDataTransmission_Delegate NP_StartDataTransmission { get; private set; }
        public NP_StopDataTransmission_Delegate NP_StopDataTransmission { get; private set; }

        private IntPtr m_hLibrary;


        public NativeClientLibrary()
        {
            // Load TrackIR Enhanced client dynamic library.
            m_hLibrary = NativeMethods.LoadLibrary( GetLibraryLocation() );
            if ( m_hLibrary == IntPtr.Zero )
            {
                throw new TrackIRException( "Error loading TrackIR client library.", new Win32Exception( Marshal.GetLastWin32Error() ) );
            }

            // Find exported functions in loaded library.
            FindAllLibraryFunctions();
        }


        ~NativeClientLibrary()
        {
            if ( m_hLibrary != IntPtr.Zero )
            {
                if ( ! NativeMethods.FreeLibrary( m_hLibrary ) )
                {
                    Console.WriteLine( "Error unloading client library: {0}", new Win32Exception( Marshal.GetLastWin32Error() ).Message );
                }
            }
        }


        private string GetLibraryLocation()
        {
			// NOTE: Imports the DLL from registered directory. Program Files (x86) by default.
            const string kLibraryLocationRegistryKey = "HKEY_CURRENT_USER\\Software\\NaturalPoint\\NaturalPoint\\NPClient Location";
            const string kLibraryLocationSubkey = "Path";

            string libraryPath = "";

            try
            {
                libraryPath = (string)Microsoft.Win32.Registry.GetValue( kLibraryLocationRegistryKey, kLibraryLocationSubkey, "" );
            }
            catch ( Exception ex )
            {
                throw new TrackIRException( "Exception trying to read client library location from registry.", ex );
            }

            libraryPath += "\\";
            libraryPath += (IntPtr.Size == 8) ? "NPClient64.dll" : "NPClient.dll";

            return libraryPath;
        }


        private void FindAllLibraryFunctions()
        {
            // This code is more verbose/less generic than I would like, largely owing to the fact that System.Delegate cannot be used as a generic constraint on FindLibraryFunction<T>.
            NP_RegisterWindowHandle = (NP_RegisterWindowHandle_Delegate)FindLibraryFunction<NP_RegisterWindowHandle_Delegate>( "NP_RegisterWindowHandle" );
            NP_UnregisterWindowHandle = (NP_UnregisterWindowHandle_Delegate)FindLibraryFunction<NP_UnregisterWindowHandle_Delegate>( "NP_UnregisterWindowHandle" );
            NP_RegisterProgramProfileID = (NP_RegisterProgramProfileID_Delegate)FindLibraryFunction<NP_RegisterProgramProfileID_Delegate>( "NP_RegisterProgramProfileID" );
            NP_QueryVersion = (NP_QueryVersion_Delegate)FindLibraryFunction<NP_QueryVersion_Delegate>( "NP_QueryVersion" );
            NP_GetSignature = (NP_GetSignature_Delegate)FindLibraryFunction<NP_GetSignature_Delegate>( "NP_GetSignature" );
            NP_GetData = (NP_GetData_Delegate)FindLibraryFunction<NP_GetData_Delegate>( "NP_GetData" );
            NP_ReCenter = (NP_ReCenter_Delegate)FindLibraryFunction<NP_ReCenter_Delegate>( "NP_ReCenter" );
            NP_StartDataTransmission = (NP_StartDataTransmission_Delegate)FindLibraryFunction<NP_StartDataTransmission_Delegate>( "NP_StartDataTransmission" );
            NP_StopDataTransmission = (NP_StopDataTransmission_Delegate)FindLibraryFunction<NP_StopDataTransmission_Delegate>( "NP_StopDataTransmission" );
        }


        private Delegate FindLibraryFunction<T>( string functionName )
        {
            IntPtr pfn = NativeMethods.GetProcAddress( m_hLibrary, functionName );
            if ( pfn == IntPtr.Zero )
            {
                throw new TrackIRException( "Error locating client library function " + functionName + ".", new Win32Exception( Marshal.GetLastWin32Error() ) );
            }
            else
            {
                return Marshal.GetDelegateForFunctionPointer( pfn, typeof( T ) );
            }
        }
    }


    internal static class NativeMethods
    {
        [DllImport( "kernel32.dll", SetLastError = true )]
        public static extern IntPtr LoadLibrary( string fileName );

        [DllImport( "kernel32.dll", SetLastError = true )]
        public static extern IntPtr GetProcAddress( IntPtr hModule, string procedureName );

        [DllImport( "kernel32.dll", SetLastError = true )]
        public static extern bool FreeLibrary( IntPtr hModule );
    }
}
