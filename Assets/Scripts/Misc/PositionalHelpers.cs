using Core;
using JetBrains.Annotations;
using UnityEngine;

namespace Misc {
    public static class PositionalHelpers {
        // used to avoid reallocation during expensive call
        private static Vector3 _terrainPosComparison;

        /**
         * Get the closest terrain tile to a given position in current (non-floating origin corrected!) world space.
         * This function ASSUMES THAT ALL TERRAIN TILES ARE EQUALLY SPACED AND SIZED and at the same Y value!
         */
        [CanBeNull]
        public static Terrain GetClosestCurrentTerrain(Vector3 toWorldPosition) {
            //Get all terrain
            var terrains = Terrain.activeTerrains;

            //If no terrains, we're done here!
            if (terrains.Length == 0)
                return null;

            //If just one, return that one terrain
            if (terrains.Length == 1)
                return terrains[0];

            //Get the closest one to the player
            var closestTerrainDistance = Mathf.Infinity;
            var terrainIndex = 0;

            for (var i = 0; i < terrains.Length; i++) {
                var terrain = terrains[i];
                var terrainPosition = terrain.transform.position;
                var terrainData = terrain.terrainData;
                _terrainPosComparison.Set(terrainPosition.x + terrainData.size.x / 2, terrainPosition.y,
                    terrainPosition.z + terrainData.size.z / 2);

                //Find the distance and check if it is lower than the last one then store it
                var dist = (_terrainPosComparison - toWorldPosition).sqrMagnitude;
                if (dist < closestTerrainDistance) {
                    closestTerrainDistance = dist;
                    terrainIndex = i;
                }
            }

            return terrains[terrainIndex];
        }

        public static Vector3 ClampPositionToTerrain(Vector3 position, float padding) {
            // if there's a terrain, clamp the height to that of the height map + padding at a minimum
            var terrain = GetClosestCurrentTerrain(position);
            if (terrain != null)
                position.y = Mathf.Max(
                    position.y,
                    terrain.transform.position.y + terrain.SampleHeight(position) + padding
                );
            return position;
        }

        // generate a valid location to spawn an object by checking in a Fibonacci sphere of ever-increasing radius until a position is found.
        public static Vector3 FindClosestEmptyPosition(Vector3 originalPosition, int objectRadius, GameObject ignoreGameObject = null) {
            Physics.SyncTransforms();

            // -1 in this instance effectively means the original position with no transformation
            var n = -1;
            var max = 40;
            var positionSphereRadius = 50;
            var hitColliders = new Collider[1];
            var testPosition = originalPosition;

            LayerMask collisionLayerMask = LayerMask.GetMask("Default", "Player", "Non-Local Player");

            // set the next position and perform a simple sphere collision check, return the true if colliding
            bool NextPositionIsObstructed() {
                if (n != -1) testPosition = originalPosition + FibSpherePosition(n, max, positionSphereRadius);

                testPosition = ClampPositionToTerrain(testPosition, objectRadius * 4);

                // check to see if any other ships have been assigned this exact start position (and are therefore currently loading also)
                var existingShipStartPosition = false;
                FdNetworkManager.Instance.ShipPlayers.ForEach(s =>
                    existingShipStartPosition = existingShipStartPosition || s.AbsoluteWorldPosition.Equals(testPosition));

                // check to see if any existing ships or geometry are in this location
                Physics.OverlapSphereNonAlloc(testPosition, objectRadius, hitColliders, collisionLayerMask);

                var hit = hitColliders[0];
                var isCollision = hit != null;

                if (ignoreGameObject && isCollision)
                    if (hit.transform.root.gameObject.GetInstanceID() == ignoreGameObject.GetInstanceID())
                        isCollision = false;

                return existingShipStartPosition || isCollision;
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