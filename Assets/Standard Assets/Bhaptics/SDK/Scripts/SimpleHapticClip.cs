using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    [CreateAssetMenu(fileName = "SimpleHapticClip", menuName = "Bhaptics/Create Simple HapticClip")]
    public class SimpleHapticClip : HapticClip
    {
        [SerializeField] private HapticClipPositionType Position = HapticClipPositionType.VestFront;

        [SerializeField] private SimpleHapticType Mode = SimpleHapticType.DotMode;

        [SerializeField] private int[] DotPoints = new int[20];

        [SerializeField] private Point[] Points = { new Point(0.5f, 0.5f, 100) };

        [Range(20, 10000)] public int TimeMillis = 1000;





        public override void Play(float intensity, float duration, float vestRotationAngleX, float vestRotationOffsetY, string identifier = "")
        {
            if (!BhapticsManager.Init)
            {
                BhapticsManager.Initialize();
                //return;
            }

            var haptic = BhapticsManager.GetHaptic();

            if (haptic == null)
            {
                return;
            }

            if (Mode == SimpleHapticType.DotMode)
            {
                haptic.Submit(keyId + identifier, BhapticsUtils.ToPositionType(Position), Convert(DotPoints), TimeMillis);
            }
            else
            {
                haptic.Submit(keyId + identifier, BhapticsUtils.ToPositionType(Position), Convert(Points), TimeMillis);
            }
        }

        public override void ResetValues()
        {
            base.ResetValues();

            DotPoints = new int[20];

            Points = new Point[] { new Point(0.5f, 0.5f, 100) };

            TimeMillis = 1000;

            Position = HapticClipPositionType.VestFront;
        }







        private static List<DotPoint> Convert(int[] points)
        {
            var result = new List<DotPoint>();

            for (var i = 0; i < points.Length; i++)
            {
                var p = points[i];

                if (p > 0)
                {
                    result.Add(new DotPoint(i, p));
                }
            }

            return result;
        }

        private static List<PathPoint> Convert(Point[] points)
        {
            var result = new List<PathPoint>();

            foreach (var point in points)
            {
                result.Add(new PathPoint(point.X, point.Y, point.Intensity));
            }

            return result;
        }
    }

    [Serializable]
    public class Point
    {
        [Range(0, 1f)] public float X;
        [Range(0, 1f)] public float Y;

        [Range(0, 100)] public int Intensity;

        public Point(float x, float y, int intensity)
        {
            X = x;
            Y = y;
            Intensity = intensity;
        }
    }

    [Serializable]
    public enum SimpleHapticType
    {
        DotMode = 1,
        PathMode = 2
    }
}