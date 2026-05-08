using AGXUnity.Model;
using System;
using UnityEngine;

namespace PWRISimulator
{
    public static class TerrainSaveUtility
    {
        public static float[,] CreateSerializableHeights(
            float[,] sourceHeights,
            float terrainSizeY,
            bool subtractRuntimeDepth,
            float maximumDepth)
        {
            int height = sourceHeights.GetLength(0);
            int width = sourceHeights.GetLength(1);
            var result = new float[height, width];
            float normalizedOffset = subtractRuntimeDepth && terrainSizeY > 0.0f ? maximumDepth / terrainSizeY : 0.0f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = Mathf.Clamp01(sourceHeights[y, x] - normalizedOffset);
                }
            }

            return result;
        }

        public static float[] CreateNativeHeightValues(float[,] normalizedHeights, float terrainSizeY, float maximumDepth)
        {
            int height = normalizedHeights.GetLength(0);
            int width = normalizedHeights.GetLength(1);
            var values = new float[height * width];
            int index = 0;

            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = width - 1; x >= 0; --x)
                {
                    values[index] = normalizedHeights[y, x] * terrainSizeY + maximumDepth;
                    index++;
                }
            }

            return values;
        }

        public static float[,] CreateVisibleHeightValues(float[,] normalizedHeights, float terrainSizeY)
        {
            int height = normalizedHeights.GetLength(0);
            int width = normalizedHeights.GetLength(1);
            var values = new float[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    values[y, x] = normalizedHeights[y, x] * terrainSizeY;
                }
            }

            return values;
        }

        public static float[,] CreateRuntimeTerrainDataHeights(
            float[,] serializedHeights,
            float terrainSizeY,
            bool addRuntimeDepth,
            float maximumDepth)
        {
            int height = serializedHeights.GetLength(0);
            int width = serializedHeights.GetLength(1);
            var result = new float[height, width];
            float normalizedOffset = addRuntimeDepth && terrainSizeY > 0.0f ? maximumDepth / terrainSizeY : 0.0f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = Mathf.Clamp01(serializedHeights[y, x] + normalizedOffset);
                }
            }

            return result;
        }

        public static float[,] GetSerializableHeights(Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            var deformableTerrain = terrain.GetComponent<DeformableTerrain>();
            bool hasRuntimeOffset = deformableTerrain?.Native != null;
            float maximumDepth = deformableTerrain != null ? deformableTerrain.MaximumDepth : 0.0f;

            return CreateSerializableHeights(heights, terrainData.size.y, hasRuntimeOffset, maximumDepth);
        }

        public static void ApplySerializedHeights(GameObject terrainObject, float[,] serializedHeights)
        {
            var terrain = terrainObject.GetComponent<Terrain>();
            var terrainData = terrain.terrainData;
            var deformableTerrain = terrainObject.GetComponent<DeformableTerrain>();
            bool hasRuntimeOffset = deformableTerrain?.Native != null;
            float maximumDepth = deformableTerrain != null ? deformableTerrain.MaximumDepth : 0.0f;
            var runtimeHeights = CreateRuntimeTerrainDataHeights(
                serializedHeights,
                terrainData.size.y,
                hasRuntimeOffset,
                maximumDepth);

            terrainData.SetHeights(0, 0, runtimeHeights);

            if (deformableTerrain?.Native != null)
            {
                var nativeHeightValues = CreateNativeHeightValues(serializedHeights, terrainData.size.y, maximumDepth);
                var nativeHeights = new agx.RealVector(nativeHeightValues.Length);
                foreach (var height in nativeHeightValues)
                {
                    nativeHeights.Add(height);
                }

                deformableTerrain.Native.setHeights(nativeHeights);
            }
        }

        public static Vector3 GetMachineRootPosition(GameObject machineRoot)
        {
            return machineRoot.transform.position;
        }

        public static Quaternion GetMachineRootRotation(GameObject machineRoot)
        {
            return machineRoot.transform.rotation;
        }

        public static bool IsSavedDumpTruckRootName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName == SpawnObject.ic120_objName)
            {
                return true;
            }

            const string idPrefix = "ic120_";
            if (!objectName.StartsWith(idPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var suffix = objectName.Substring(idPrefix.Length);
            return int.TryParse(suffix, out var id) && id >= 0;
        }
    }
}
