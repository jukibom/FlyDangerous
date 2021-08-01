using JetBrains.Annotations;
using UnityEngine;

namespace Misc {
    public static class PositionalHelpers {
        
        [CanBeNull] public static Terrain GetClosestCurrentTerrain(Vector3 position) {
            //Get all terrain
            Terrain[] terrains = Terrain.activeTerrains;

            //Make sure that terrains length is ok
            if (terrains.Length == 0)
                return null;

            //If just one, return that one terrain
            if (terrains.Length == 1)
                return terrains[0];

            //Get the closest one to the player
            float lowDist = (terrains[0].GetPosition() - position).sqrMagnitude;
            var terrainIndex = 0;

            for (int i = 1; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                Vector3 terrainPos = terrain.GetPosition();

                //Find the distance and check if it is lower than the last one then store it
                var dist = (terrainPos - position).sqrMagnitude;
                if (dist < lowDist)
                {
                    lowDist = dist;
                    terrainIndex = i;
                }
            }
            return terrains[terrainIndex];
        }
        
        public static Vector3 ClampPositionToTerrain(Vector3 position, float padding) {
            // if there's a terrain, clamp the height to that of the height map + padding at a minimum
            var terrain = GetClosestCurrentTerrain(position);
            if (terrain != null) {
                position.y = Mathf.Max(
                    position.y, 
                    terrain.transform.position.y + terrain.SampleHeight(position) + padding
                );
            }
            return position;
        }

        // generate a valid location to spawn an object by checking in a Fibonacci sphere of ever-increasing radius until a position is found.
        public static Vector3 FindClosestEmptyPosition(Vector3 originalPosition, int objectRadius) {
            // -1 in this instance effectively means the original position with no transformation
            int n = -1;
            int max = 40;
            int positionSphereRadius = 50;
            Collider[] hitColliders = new Collider[1];
            Vector3 testPosition = originalPosition;

            // check against everything except UI elements
            int collisionLayerMask = ~(1 << 5);

            // set the next position and perform a simple sphere collision check, return the number of
            bool NextPositionIsObstructed() {
                if (n != -1) {
                    testPosition = originalPosition + FibSpherePosition(n, max, positionSphereRadius);   
                }

                Physics.OverlapSphereNonAlloc(testPosition, objectRadius, hitColliders, collisionLayerMask);
                testPosition = ClampPositionToTerrain(testPosition, objectRadius);

                return hitColliders[0] != null;
            }

            while (NextPositionIsObstructed()) {
                // clear the colliders array, increment our counter and try again
                hitColliders[0] = null;
                n++;
                if (n == max) {
                    n = 0;
                    positionSphereRadius += 20;
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