using System;
using System.Collections;
using AGXUnity;
using AGXUnity.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PWRISimulator
{
    public static class Zx200ObjectUtility
    {
        public const string BaseObjectName = "zx200";
        public const string PrefVariantObjectName = SpawnObject.zx200_objName;
        private const string IdPrefix = BaseObjectName + "_";

        public static bool IsZx200Name(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName == BaseObjectName || objectName == PrefVariantObjectName)
            {
                return true;
            }

            if (!objectName.StartsWith(IdPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var suffix = objectName.Substring(IdPrefix.Length);
            return int.TryParse(suffix, out var id) && id >= 0;
        }

        public static GameObject FindZx200Object()
        {
            var prefVariantObject = GameObject.Find(PrefVariantObjectName);
            if (prefVariantObject != null)
            {
                return prefVariantObject;
            }

            var baseObject = GameObject.Find(BaseObjectName);
            if (baseObject != null)
            {
                return baseObject;
            }

            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    if (IsZx200Name(rootGameObject.name))
                    {
                        return rootGameObject;
                    }
                }
            }

            return null;
        }

        public static IEnumerator AttachShovelToTerrainWhenInitialized(DeformableTerrain terrain, GameObject shovelRoot)
        {
            if (terrain?.Native == null || shovelRoot == null)
            {
                yield break;
            }

            yield return null;

            var shovel = shovelRoot.GetComponentInChildren<DeformableTerrainShovel>();
            if (shovel == null)
            {
                yield break;
            }

            yield return new WaitUntil(() => shovel == null || shovel.State != ScriptComponent.States.INITIALIZING);

            if (shovel == null || terrain.Native == null || shovel.State == ScriptComponent.States.DESTROYED)
            {
                yield break;
            }

            if (shovel.State != ScriptComponent.States.INITIALIZED)
            {
                shovel = shovel.GetInitialized<DeformableTerrainShovel>();
            }

            if (shovel?.Native != null)
            {
                terrain.Native.add(shovel.Native);
            }
        }
    }
}
