using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    public interface IHaptic
    {
        /// <summary>
        /// If there is no haptic for more than 5 seconds due to a performance issue, it returns false.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool IsConnect(PositionType type);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isLeft">Value used for devices with left and right sides.(Default: Left)</param>
        /// <returns></returns>
        bool IsConnect(HapticDeviceType type, bool isLeft = true);

        bool IsPlaying(string key);
        
        bool IsFeedbackRegistered(string key);

        bool IsPlaying();

        void RegisterTactFileStr(string key, string tactFileStr);

        void RegisterTactFileStrReflected(string key, string tactFileStr);

        void Submit(string key, PositionType position, List<DotPoint> points, int durationMillis);

        void Submit(string key, PositionType position, List<PathPoint> points, int durationMillis);

        void SubmitRegistered(string key, string altKey, ScaleOption option);

        void SubmitRegistered(string key, string altKey, RotationOption rOption, ScaleOption sOption);

        void SubmitRegistered(string key);

        void SubmitRegistered(string key, int startTimeMillis);

        void TurnOff(string key);

        void TurnOff();

        void Dispose();

        int[] GetCurrentFeedback(PositionType pos);
    }
}
