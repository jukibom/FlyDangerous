//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;


namespace NaturalPoint.TrackIR
{
    /// <summary>
    /// Represents the position and orientation data from a single frame of head tracking.
    /// </summary>
    /// <remarks>
    /// TrackIR coordinate system (from the tracked user's perspective, facing the display + TrackIR device):
    ///     +X = Left
    ///     +Y = Up
    ///     +Z = Back
    /// </remarks>
    public struct Pose
    {
        public Quaternion Orientation;
        public Vector3 PositionMeters;

        public static readonly Pose Identity = new Pose { Orientation = Quaternion.Identity, PositionMeters = Vector3.Zero };

        // Minimal nested quaternion structure.
        public struct Quaternion
        {
            public float W;
            public float X;
            public float Y;
            public float Z;

            public static readonly Quaternion Identity = new Quaternion( 1.0f, 0.0f, 0.0f, 0.0f );

            public Quaternion( float w, float x, float y, float z )
            {
                W = w;
                X = x;
                Y = y;
                Z = z;
            }

            // Returns a quaternion representing Tait-Bryan intrinsic rotation angles in z-y'-x'' sequence.
            public static Quaternion FromTaitBryanIntrinsicZYX( float zRadians, float yRadians, float xRadians )
            {
                double sz = Math.Sin( zRadians * 0.5f );
                double cz = Math.Cos( zRadians * 0.5f );
                double sy = Math.Sin( yRadians * 0.5f );
                double cy = Math.Cos( yRadians * 0.5f );
                double sx = Math.Sin( xRadians * 0.5f );
                double cx = Math.Cos( xRadians * 0.5f );

                return new Quaternion
                {
                    W = (float)(  (cx * cy * cz) + (sx * sy * sz) ),
                    X = (float)(  (sx * cy * cz) - (cx * sy * sz) ),
                    Y = (float)(  (cx * sy * cz) + (sx * cy * sz) ),
                    Z = (float)( -(sx * sy * cz) + (cx * cy * sz) ),
                };
            }
        }

        // Minimal nested 3D vector structure.
        public struct Vector3
        {
            public float X;
            public float Y;
            public float Z;

            public static readonly Vector3 Zero = new Vector3( 0.0f, 0.0f, 0.0f );

            public Vector3( float x, float y, float z )
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
    }


    /// <summary>
    /// Base class for all exceptions raised by TrackIR code.
    /// </summary>
    public class TrackIRException : System.Exception
    {
        public TrackIRException()
        {
        }

        public TrackIRException( string message )
            : base( message )
        {
        }

        public TrackIRException( string message, Exception inner )
            : base( message, inner )
        {
        }
    }
}
