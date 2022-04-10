using UnityEngine;
using AGXUnity;
using AGXUnity.Model;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    public class DeformableTerrainCommands : ScriptComponent
    {
        [SerializeField] private DeformableTerrain deformableTerrain;
        
        void Reset()
        {
            deformableTerrain = GetComponent<DeformableTerrain>();
            if (deformableTerrain == null)
                deformableTerrain = FindObjectOfType<DeformableTerrain>();
        }

        protected override bool Initialize()
        {
            if(deformableTerrain == null)
                deformableTerrain = GetComponent<DeformableTerrain>();

            return base.Initialize();
        }

        public void TriggerAvalancheAll()
        {
            if (deformableTerrain?.GetInitialized<DeformableTerrain>()?.Native == null)
            {
                Debug.LogError($"{name} : Failed to trigger avalanche because terrain native could not be found.");
                return;
            }

            Debug.Log($"{name} : Triggering avalanche.");

            deformableTerrain.Native.triggerForceAvalancheAll();
        }

        public void RemoveAllParticles()
        {
            if (deformableTerrain?.GetInitialized<DeformableTerrain>()?.Native == null)
            {
                Debug.LogError($"{name} : Failed to remove all particles because terrain native could not be found.");
                return;
            }
            
            Debug.Log($"{name} : Removing all particles.");

            var soilSimulation = deformableTerrain.Native.getSoilSimulationInterface();
            var particles = soilSimulation.getSoilParticles();
            int particleCount = (int) particles.size();

            for (int i = 0; i < particleCount; ++i)
            {
                var particle = particles.at((uint) i);
                soilSimulation.removeSoilParticle(particle);
                particle.ReturnToPool();
            }
        }

        public void ResetHeights()
        {
            if (deformableTerrain?.GetInitialized<DeformableTerrain>() == null)
            {
                Debug.LogError($"{name} : Failed to reset heights because terrain is not set.");
                return;
            }
            RemoveAllParticles();
            deformableTerrain.ResetHeights();
        }

        public void MoveTerrainHeightsVertically(float distance, bool abortIfHeightsBecomeNegative = true)
        {
            if (deformableTerrain?.Terrain?.terrainData == null)
            {
                Debug.LogError($"{name} : Failed to move terrain heights because terrain data could not be found.");
                return;
            }

            var terrainData = deformableTerrain?.Terrain?.terrainData;
            var heightmapOffset = distance / terrainData.size.y;            
            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

            if (abortIfHeightsBecomeNegative)
            {
                for (int y = 0; y < terrainData.heightmapResolution; y++)
                {
                    for (int x = 0; x < terrainData.heightmapResolution; x++)
                    {
                        if (heights[y, x] + heightmapOffset < 0.0)
                        {
                            Debug.LogWarning($"{name} : Aborted moving terrain heights down because some heights will " +
                                             $"become negative and therefore automatically clamped to zero " +
                                             $"(i.e. terrain cannot get lower without flattening it).");
                            return;
                        }
                    }
                }
            }

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    heights[y, x] = heights[y, x] + heightmapOffset;
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        public void MoveTerrainHeightsUp()
        {
            if (deformableTerrain == null)
            {
                Debug.LogError($"{name} : Failed to move terrain heights up because terrain is not set.");
                return;
            }
            MoveTerrainHeightsVertically(deformableTerrain.MaximumDepth);
        }

        public void MoveTerrainHeightsDown()
        {
            if (deformableTerrain == null)
            {
                Debug.LogError($"{name} : Failed to move terrain heights down because terrain is not set.");
                return;
            }
            MoveTerrainHeightsVertically(-deformableTerrain.MaximumDepth);
        }

    }
#if UNITY_EDITOR
    [CustomEditor(typeof(DeformableTerrainCommands))]
    public class TerrainCommandsEditor : Editor
    {
        DeformableTerrainCommands obj { get { return target as DeformableTerrainCommands; } }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Commands", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Remove Particles", GUILayout.Width(200)))
            {
                obj.RemoveAllParticles();
            }

            if (GUILayout.Button("Reset Heights", GUILayout.Width(200)))
            {
                obj.ResetHeights();
            }

            if (GUILayout.Button("Trigger Avalanche", GUILayout.Width(200)))
            {
                obj.TriggerAvalancheAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Commands", EditorStyles.boldLabel);

            GUI.enabled = !Application.isPlaying;

            if (GUILayout.Button("Move Terrain Heights Up     (by Max Depth)", GUILayout.Width(280)))
            {
                obj.MoveTerrainHeightsUp();
            }

            if (GUILayout.Button("Move Terrain Heights Down (by Max Depth)", GUILayout.Width(280)))
            {
                obj.MoveTerrainHeightsDown();
            }

        }
    }
#endif
}
