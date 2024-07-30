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
        protected override bool Initialize()
        {
            if (deformableTerrain == null)
                deformableTerrain = GetComponent<DeformableTerrain>();
            
            for (int i = 0; i < deformableTerrain.Shovels.Length; i++)
            {
                // 左右側面と背面を削減
                deformableTerrain.Shovels[i].Native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_RIGHT).setEnable(false);
                deformableTerrain.Shovels[i].Native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_LEFT).setEnable(false);
                deformableTerrain.Shovels[i].Native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_BACK).setEnable(false);
            }

            return base.Initialize();
        }

    }
}
