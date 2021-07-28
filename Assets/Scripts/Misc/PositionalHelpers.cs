using UnityEngine;

namespace Misc {
    public static class PositionalHelpers {

        // generate a valid location to spawn an object by checking in a Fibonacci sphere of ever-increasing radius until a position is found.
        public static Vector3 FindClosestEmptyPosition(Vector3 originalPosition, int objectRadius) {
            // -1 in this instance effectively means the original position with no transformation
            int n = -1;
            int max = 20;
            int radius = 50;
            Collider[] hitColliders = new Collider[1];
            Vector3 testPosition = originalPosition;

            // check against everything except UI elements
            int collisionLayerMask = ~(1 << 5);

            // set the next position and perform a simple sphere collision check, return the number of
            bool NextPositionIsObstructed() {
                if (n != -1) {
                    testPosition = originalPosition + FibSpherePosition(n, max, radius);   
                }
                Physics.OverlapSphereNonAlloc(testPosition, objectRadius, hitColliders, collisionLayerMask);
                return hitColliders[0] != null;
            }

            while (NextPositionIsObstructed()) {
                // clear the colliders array, increment our counter and try again
                hitColliders[0] = null;
                n++;
                if (n == max) {
                    n = 0;
                    radius += 20;
                }

                Debug.Log("Failed to position ship at " + testPosition);
            }

            Debug.Log("Positioning ship at " + testPosition);

            return testPosition;
        }
        
        // return a point on a fibonacci sphere
        // Thanks to https://medium.com/@vagnerseibert/distributing-points-on-a-sphere-6b593cc05b42
        private static Vector3 FibSpherePosition(int n, int max, float radius) {
            var k = n + .5f;

            var phi = Mathf.Acos(1f - 2f * k / max);
            var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

            var x = Mathf.Cos(theta) * Mathf.Sin(phi);
            var y = Mathf.Sin(theta) * Mathf.Sin(phi);
            var z = Mathf.Cos(phi);

            return new Vector3(x, y, z) * radius;
        }
    }
}