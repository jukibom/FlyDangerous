//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;
using NaturalPoint.TrackIR;
using UnityEngine;
using Pose = NaturalPoint.TrackIR.Pose;

/// <summary>
///     Demonstrates driving a GameObject's translation and rotation according to head tracking data provided by the
///     TrackIR Enhanced API.
/// </summary>
public class TrackIRComponent : MonoBehaviour {
    /// <summary>
    ///     The ID provided to you by NaturalPoint, unique to your application. Determines the title displayed in the
    ///     TrackIR software.
    /// </summary>
    public UInt16 AssignedApplicationId = 0;

    /// <summary>
    ///     After this many seconds without any new data, tracking is considered to have been lost, and the position and
    ///     orientation will smoothly interpolate back to center/identity.
    /// </summary>
    public float TrackingLostTimeoutSeconds = 3.0f;

    /// <summary>
    ///     How many seconds it takes the position and orientation to transition back to center/identity after tracking
    ///     has been lost.
    /// </summary>
    public float TrackingLostRecenterDurationSeconds = 1.0f;

    /// <summary>
    ///     Keeps track of how long it's been since we last got new head tracking data during an update.
    /// </summary>
    float m_staleDataDuration;

    /// <summary>
    ///     Helper class that simplifies interacting with the TrackIR Enhanced API.
    /// </summary>
    Client m_trackirClient;


    /// MonoBehaviour message.
    void Start() {
        InitializeTrackIR();
    }


    /// MonoBehaviour message.
    void Update() {
        UpdateTrackIR();
    }


    /// MonoBehaviour message.
    void OnApplicationQuit() {
        ShutDownTrackIR();
    }


    /// <summary>
    ///     Attempts to instantiate the TrackIR client object using the specified application ID as well as the handle for
    ///     Unity's foreground window.
    /// </summary>
    /// <remarks>
    ///     If the user does not have the TrackIR software installed, the client constructor will throw, m_trackirClient
    ///     will be null, and subsequent update/shutdown calls will early out accordingly.
    /// </remarks>
    private void InitializeTrackIR() {
        try {
            m_trackirClient = new Client(AssignedApplicationId, TrackIRNativeMethods.GetUnityHwnd());
        }
        catch (TrackIRException ex) {
            Debug.LogWarning("TrackIR Enhanced API not available.");
            Debug.LogException(ex);
        }
    }


    /// <summary>
    ///     Checks for the availability of new head tracking data. If new data is available, it's applied to this
    ///     GameObject's position and orientation.
    /// </summary>
    /// <remarks>
    ///     If no new data is available for longer than the configured timeout, tracking is considered lost, and we
    ///     gradually recenter the object's position and orientation (interpolating both to identity over the duration
    ///     specified by <see cref="TrackingLostRecenterDurationSeconds" />).
    /// </remarks>
    private void UpdateTrackIR() {
        if (m_trackirClient != null) {
            bool bNewPoseAvailable = false;

            // UpdatePose() could throw if it attempts and fails to reconnect.
            // This should be rare. We'll treat it as non-recoverable.
            try {
                bNewPoseAvailable = m_trackirClient.UpdatePose();
            }
            catch (TrackIRException ex) {
                Debug.LogError("TrackIR.Client.UpdatePose threw an exception.");
                Debug.LogException(ex);

                m_trackirClient.Disconnect();
                m_trackirClient = null;
                return;
            }

            Pose pose = m_trackirClient.LatestPose;

            // TrackIR's X and Z axes are inverted compared to Unity, equivalent to a 180 degree rotation about the Y axis.
            Vector3 posePosition = new Vector3(
                -pose.PositionMeters.X,
                pose.PositionMeters.Y,
                -pose.PositionMeters.Z
            );

            Quaternion poseOrientation = new Quaternion(
                -pose.Orientation.X,
                pose.Orientation.Y,
                -pose.Orientation.Z,
                pose.Orientation.W
            );

            if (bNewPoseAvailable) {
                // New data was available, apply it directly here.
                transform.localPosition = posePosition;
                transform.localRotation = poseOrientation;
                m_staleDataDuration = 0.0f;
            }
            else {
                // Data was stale. If it's been stale for too long, smoothly recenter the camera.
                m_staleDataDuration += Time.deltaTime;

                if (m_staleDataDuration > TrackingLostTimeoutSeconds) {
                    float recenterFraction = Mathf.Clamp01((m_staleDataDuration - TrackingLostTimeoutSeconds) / TrackingLostRecenterDurationSeconds);
                    recenterFraction = Mathf.SmoothStep(0.0f, 1.0f, recenterFraction);
                    transform.localPosition = Vector3.Lerp(posePosition, Vector3.zero, recenterFraction);
                    transform.localRotation = Quaternion.Slerp(poseOrientation, Quaternion.identity, recenterFraction);
                }
            }
        }
    }


    /// <summary>
    ///     Cleans up by unregistering this application with the TrackIR software.
    /// </summary>
    private void ShutDownTrackIR() {
        if (m_trackirClient != null) {
            m_trackirClient.Disconnect();
        }
    }
}


internal static class TrackIRNativeMethods {
    public delegate bool EnumThreadWindowsCallbackDelegate(IntPtr hwnd, IntPtr lParamContext);

    [DllImport("kernel32.dll")]
    public static extern UInt32 GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(UInt32 dwThreadId, EnumThreadWindowsCallbackDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Int32 GetClassName(IntPtr hWnd, StringBuilder lpClassName, Int32 nMaxCount);


    /// <summary>
    ///     Enumerates all windows belonging to the calling thread, looking for one matching a known Unity window class
    ///     name. Should be called from the main thread.
    /// </summary>
    /// <returns>The HWND handle corresponding to Unity's foreground window.</returns>
    public static IntPtr GetUnityHwnd() {
        IntPtr foundHwnd = IntPtr.Zero;

        StringBuilder outClassnameBuilder = new StringBuilder(32);

        // Search all windows belonging to the current thread, and filter down by looking for known Unity window
        // class names.
        EnumThreadWindowsCallbackDelegate enumCallback = (IntPtr hwnd, IntPtr lParamContext) => {
            // Clear the string builder's contents with each iteration.
            outClassnameBuilder.Length = 0;

            GetClassName(hwnd, outClassnameBuilder, outClassnameBuilder.Capacity);
            string hwndClass = outClassnameBuilder.ToString();
            if (hwndClass == "UnityWndClass" || hwndClass == "UnityContainerWndClass") {
                // We found the right window; stop enumerating.
                foundHwnd = hwnd;
                return false;
            }

            // Continue enumeration.
            return true;
        };

        IntPtr enumCallbackContext = IntPtr.Zero;
        EnumThreadWindows(GetCurrentThreadId(), enumCallback, enumCallbackContext);

        if (foundHwnd == IntPtr.Zero) {
            Debug.LogError("Unable to retrieve Unity window handle.");
        }

        return foundHwnd;
    }
}