using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Bloxel
{
    public class VoxelClient : MonoBehaviour
    {
        public bool EnableLogs = true;
        public VoxelStore store;
        public Transform player;
        [Range(1, 32)] public int chunkGenerationDistance = 32;
        public int chunksPerFrame = 5;
        public bool cleanupChunks = true;

        private int3 lastCenter;
        private int chunksPlayerHasTraversed = 0;
        private int currentRadius = 0;
        private Coroutine floodfillCoroutine;
        #region Floodfill terrain
        private void Start()
        {
            FillCenter();
            store.LoadStorable(GetCenter());
            floodfillCoroutine = StartCoroutine(Floodfill());
        }

        private void Update()
        {
            int3 center = GetCenter();
            if (lastCenter.x != center.x || lastCenter.z != center.z)
            {
                // number of chunks the player has moved
                chunksPlayerHasTraversed = Mathf.RoundToInt(math.distance(lastCenter, GetCenter()));

                lastCenter = center;

                FillCenter();

                StopAllCoroutines();

                if (cleanupChunks)
                    Cleanup();

                floodfillCoroutine = StartCoroutine(Floodfill());
            }
        }

        private void OnDisable()
        {
            StopCoroutine(floodfillCoroutine);
        }

        private void FillCenter()
        {
            int3 center = GetCenter();
            store.LoadStorable(center);
        }

        private IEnumerator Floodfill()
        {
            // decrease currentRadius by number of chunks the player has traversed
            currentRadius -= chunksPlayerHasTraversed;

            // if below negative (the player has fast enough that they outran the chunkGenerationDistance radius) 
            if (currentRadius < 0)
            {
                // reset to zero
                currentRadius = 0;
            }

            int3 center = GetCenter();

            int chunksLoaded = 0;

            while (currentRadius <= chunkGenerationDistance)
            {

                // forward (+Z)
                for (int i = -currentRadius; i <= currentRadius - 1; i++)
                {
                    if (chunksLoaded > chunksPerFrame)
                    {
                        chunksLoaded = 0;
                        yield return null;
                    }

                    var key = new int3(i, 0, currentRadius) + center;
                    store.LoadStorable(key);
                    chunksLoaded++;
                }

                // right (+X)
                for (int i = -currentRadius + 1; i <= currentRadius; i++)
                {
                    if (chunksLoaded > chunksPerFrame)
                    {
                        chunksLoaded = 0;
                        yield return null;
                    }

                    var key = new int3(currentRadius, 0, i) + center;
                    store.LoadStorable(key);
                    chunksLoaded++;
                }

                // backward (-Z)
                for (int i = -currentRadius + 1; i <= currentRadius; i++)
                {
                    if (chunksLoaded > chunksPerFrame)
                    {
                        chunksLoaded = 0;
                        yield return null;
                    }

                    var key = new int3(i, 0, -currentRadius) + center;
                    store.LoadStorable(key);
                    chunksLoaded++;
                }

                // left (-X)
                for (int i = -currentRadius; i <= currentRadius - 1; i++)
                {
                    if (chunksLoaded > chunksPerFrame)
                    {
                        chunksLoaded = 0;
                        yield return null;
                    }

                    var key = new int3(-currentRadius, 0, i) + center;
                    store.LoadStorable(key);
                    chunksLoaded++;
                }

                currentRadius += 1;

                yield return null;
            }
        }

        private void Cleanup()
        {
            float maxDistance = new Vector3(chunkGenerationDistance * VoxelTables.ChunkResolution, 0, chunkGenerationDistance * VoxelTables.ChunkResolution).magnitude;

            List<int3> idsToRemove = new List<int3>();

            var halfChunkSize = new Vector3(VoxelTables.ChunkResolution / 2, 0, VoxelTables.ChunkResolution / 2);

            // go through all chunks
            foreach (var id in store.GetIDs())
            {
                Vector3 pos = new Vector3(id.x * VoxelTables.ChunkResolution, 0, id.z * VoxelTables.ChunkResolution) + halfChunkSize;
                
                // distance from player
                float distanceFromPlayer = Vector3.Distance(pos, new Vector3(player.position.x, 0, player.position.z));

                // if outside of max distance
                if (distanceFromPlayer > maxDistance)
                {
                    idsToRemove.Add(id);
                }
            }

            // go through all keys
            foreach (var id in idsToRemove)
            {
                StartCoroutine(store.UnloadStorableAsync(id));
            }
        }

        private int3 GetCenter()
        {
            return new int3(
                Mathf.FloorToInt(
                    player.position.x / VoxelTables.ChunkResolution
                ),
                0,
                Mathf.FloorToInt(
                    player.position.z / VoxelTables.ChunkResolution
                )
            );
        }

        private void OnDrawGizmosSelected()
        {
            // offset by half a chunk since chunk position is not centered on chunks
            var halfChunkSize = new Vector3(VoxelTables.ChunkResolution / 2, 0, VoxelTables.ChunkResolution / 2);

            // the size of the wire cube
            var size = new Vector3(chunkGenerationDistance * VoxelTables.ChunkResolution * 2, 0, chunkGenerationDistance * VoxelTables.ChunkResolution * 2);

            // set color to red
            Gizmos.color = Color.red;

            Vector3 center = new Vector3
            (
                GetCenter().x * VoxelTables.ChunkResolution, 0, GetCenter().z * VoxelTables.ChunkResolution
            );

            // draw wire cube
            // offset RoundedPlayerPosition by halfChunkSize
            Gizmos.DrawWireCube(
                center + halfChunkSize,
                size
            );

            // get max radius
            float maxRadius = new Vector3(chunkGenerationDistance * VoxelTables.ChunkResolution, 0, chunkGenerationDistance * VoxelTables.ChunkResolution).magnitude;

            // draw wire sphere
            Gizmos.DrawWireSphere(center + halfChunkSize, maxRadius);
        }
        #endregion
    }
}