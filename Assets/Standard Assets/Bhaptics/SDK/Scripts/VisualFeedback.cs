using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Bhaptics.Tact.Unity
{
    public class VisualFeedback : MonoBehaviour
    {
        public HapticClipPositionType devicePos = HapticClipPositionType.Head;
        public Transform motorContainer;
        public Gradient motorFeedbackGradient;


        private Transform[] motors;
        private float motorScaleOffset = 1f;



        void Start()
        {
            if (motorContainer == null)
            {
                BhapticsLogger.LogError("VisualFeedback.cs - Start() / motorContainer is null");

                return;
            }

            var originSiblingIndex = motorContainer.GetSiblingIndex();

            var tmpMotorContainer = Instantiate(motorContainer, motorContainer.parent);
            tmpMotorContainer.localPosition = motorContainer.localPosition;
            tmpMotorContainer.localRotation = motorContainer.localRotation;
            tmpMotorContainer.SetSiblingIndex(originSiblingIndex);

            var tmpList = new List<Transform>();

            for (int i = 0; i < motorContainer.childCount; ++i)
            {
                tmpList.Add(motorContainer.GetChild(i));
            }

            motors = tmpList.ToArray();

            UpdateFeedback(new HapticFeedback(BhapticsUtils.ToPositionType(devicePos), new byte[motors.Length]));
        }






        public void UpdateFeedback(HapticFeedback feedback)
        {
            UpdateFeedback(System.Array.ConvertAll(feedback.Values, System.Convert.ToInt32));
        }

        public void UpdateFeedback(int[] feedbackValues)
        {
            if (motors == null)
            {
                return;
            }
            for (int i = 0; i < motors.Length; i++)
            {
                var motor = motors[i];

                if (motor == null)
                {
                    return;
                }

                var intensity = feedbackValues[i] * 0.01f;

                if (intensity > 0f)
                {
                    intensity = intensity + motorScaleOffset;
                }

                motor.transform.localScale = Vector3.one * intensity;

                var motorImage = motor.GetComponent<Image>();

                if (motorImage != null)
                {
                    motorImage.color = motorFeedbackGradient.Evaluate(intensity);
                }
            }
        }
    }
}