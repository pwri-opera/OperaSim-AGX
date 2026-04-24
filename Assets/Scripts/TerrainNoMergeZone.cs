using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// AGXUnity.Shapeのコンポーネントを持つGameObjectに追加すると、その形状の中に入っている粒子がTerrainとマージできないように
    /// なる。参考：AgxTerrain.Terrain.addNoMergeZoneToGeometry
    /// </summary>
    /// <remarks>現時点では、実際的に指示した形状だけじゃなく、その形状が広げるAxis Aligned Bounding Boxの容積が使用される。
    /// つまり、実際の形状より大きい容積の可能性。</remarks>
    public class TerrainNoMergeZone : ScriptComponent
    {
        [Tooltip("The distance by which to extend the no-merge volume from the original shape volume.")]
        [Range(0.0f, 10.0f)]
        public double extensionDistance = 0.0;
        [Tooltip("Apply no-merge zone to all shapes in the sub-tree of this game object.")]
        public bool propagateToChildren = false;

        [Header("Overrides (auto-assigned on Play)")]
        public DeformableTerrain terrain;

        bool isInitialized = false;
        bool isQuitting = false;

        readonly Dictionary<Shape, agxTerrain.ForbiddenBound> noMergeBounds = new Dictionary<Shape, agxTerrain.ForbiddenBound>();

        protected override bool Initialize()
        {
            isInitialized = true;

            if (terrain == null)
                terrain = FindObjectOfType<DeformableTerrain>(false);
            
            bool success = AddOrRemoveNoMergeZone(remove: false);

            return base.Initialize() && success;
        }

        protected override void OnEnable()
        {        
            if (isInitialized)
                AddOrRemoveNoMergeZone(remove: false);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (!isQuitting)
                AddOrRemoveNoMergeZone(remove: true);
            base.OnDisable();
        }

        protected override void OnApplicationQuit()
        {
            isQuitting = true;
            base.OnApplicationQuit();
        }

        bool AddOrRemoveNoMergeZone(bool remove = false)
        {
            //Debug.Log(name + ": " + (remove ? "Removing" : "Adding") + " no merge terrain zone.");

            bool success = false;
            if (terrain?.GetInitialized<DeformableTerrain>() != null)
            {
                success = true;
                List<Shape> shapes = new List<Shape>();
                gameObject.GetComponentsInChildren<Shape>(remove ? true : false, shapes);
                foreach (Shape shape in shapes)
                    success = AddOrRemoveNoMergeZone(shape, remove) && success;
            }

            if (!success)
                Debug.LogError(name + " : Failed to " + (remove ? "remove" : "add") + " no merge terrain zone.");

            return success;
        }

        bool AddOrRemoveNoMergeZone(Shape shape, bool remove = false)
        {
            if (shape.GetInitialized<Shape>() == null)
                return false;

            bool success = true;

            if (noMergeBounds.TryGetValue(shape, out var forbiddenBound) && forbiddenBound != null)
            {
                terrain.Native.removeForbiddenBound(forbiddenBound);
                noMergeBounds.Remove(shape);
            }

            if (!remove)
            {
                shape.NativeGeometry.updateBoundingVolume();
                var boundingVolume = shape.NativeGeometry.getBoundingVolume();
                var localTransform = shape.NativeGeometry.getLocalTransform();
                var createdBound = new agxTerrain.ForbiddenBound(
                    boundingVolume,
                    localTransform,
                    shape.NativeGeometry.getFrame());
                if (createdBound != null)
                {
                    terrain.Native.addForbiddenBound(createdBound);
                    noMergeBounds[shape] = createdBound;
                }
                success = createdBound != null && success;
            }

            if (!success)
                Debug.LogWarning($"{name} : Failed to " + (remove ? "remove" : "add") + $" shape \"{shape.name}\" " +
                                  "to no merge terrain zone.");
            return success;
        }
    }
}
