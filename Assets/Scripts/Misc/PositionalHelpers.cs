using UnityEngine;

namespace Misc {
    public static class PositionalHelpers {

        // generate a valid location to spawn an object by checking in a Fibonacci sphere of ever-increasing radius until a position is found.
        public static Vector3 FindClosestEmptyPosition(Vector3 position, int objectRadius) {

            // if the requested position is clear, we have no work to do.
            // TODO: add a layer mask specifically for the local player to avoid checking against itself (starts at 0,0,0 - if the map does too then this will fail)
            var hits = Physics.OverlapSphere(position, objectRadius);
            if (hits.Length == 0) {
                return position;
            }
            
            int n = 0;
            int max = 20;
            int radius = 50;
            Collider[] hitColliders = new Collider[1];
            Vector3 testPosition = position;

            // set the next position and perform a simple sphere collision check, return the number of
            bool CheckNextPositionIsClear() {
                testPosition = position + FibSpherePosition(n, max, radius);
                Physics.OverlapSphereNonAlloc(testPosition, objectRadius, hitColliders);
                return hitColliders[0] == null;
            }

            while (!CheckNextPositionIsClear()) {
                // clear the colliders array, increment our counter and try again
                hitColliders[0] = null;
                n++;
                if (n == max) {
                    n = 0;
                    radius += 20;
                }
            }

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