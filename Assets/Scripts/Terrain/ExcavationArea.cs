using AGXUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity.Model;

namespace PWRISimulator
{
    /// <summary>
    /// Deformable Terrain Shovelの掘削面を設定するクラス
    /// 掘削面を減らすことで土粒子の発生数を削減し、計算を軽くする狙い
    /// </summary>
    public class ExcavationArea : ScriptComponent
    {
        DeformableTerrain deformableTerrain;
        [SerializeField] DeformableTerrainShovel[] shovels;

        protected override bool Initialize()
        {
            if (deformableTerrain == null)
                deformableTerrain = GetComponent<DeformableTerrain>();

            if (shovels == null || shovels.Length == 0)
                shovels = FindObjectsOfType<DeformableTerrainShovel>(true);

            ApplyToInitializedShovels();
            ScriptComponent.OnInitialized -= OnScriptComponentInitialized;
            ScriptComponent.OnInitialized += OnScriptComponentInitialized;

            return base.Initialize();
        }

        protected override void OnDestroy()
        {
            ScriptComponent.OnInitialized -= OnScriptComponentInitialized;
            base.OnDestroy();
        }

        void OnScriptComponentInitialized(ScriptComponent component)
        {
            if (!(component is DeformableTerrainShovel shovel))
                return;

            if (shovels == null || System.Array.IndexOf(shovels, shovel) < 0)
                return;

            ApplyExcavationArea(shovel);
        }

        void ApplyToInitializedShovels()
        {
            if (shovels == null)
                return;

            foreach (var shovel in shovels)
                ApplyExcavationArea(shovel);
        }

        void ApplyExcavationArea(DeformableTerrainShovel shovel)
        {
            if (shovel == null || shovel.State != States.INITIALIZED || shovel.Native == null)
                return;

            shovel.Native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_RIGHT).setEnable(false);
            shovel.Native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_LEFT).setEnable(false);
            shovel.Native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_BACK).setEnable(false);
        }

    }
}
