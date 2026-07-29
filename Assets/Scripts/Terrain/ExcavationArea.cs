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

            if (deformableTerrain == null)
                return base.Initialize();

            // ScriptComponent の初期化順は保証されないため、地形の初期化を待つ。
            // 待たないと Shovels の Native が未生成のことがある。
            deformableTerrain = deformableTerrain.GetInitialized<DeformableTerrain>();
            if (deformableTerrain == null)
                return base.Initialize();

            foreach (var shovel in deformableTerrain.Shovels)
            {
                // 参照切れや初期化失敗のショベルは飛ばす
                if (shovel == null)
                    continue;

                var native = shovel.GetInitialized<DeformableTerrainShovel>()?.Native;
                if (native == null)
                    continue;

                // 左右側面と背面を削減
                native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_RIGHT).setEnable(false);
                native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_LEFT).setEnable(false);
                native.getExcavationSettings(agxTerrain.Shovel.ExcavationMode.DEFORM_BACK).setEnable(false);
            }

            return base.Initialize();
        }

    }
}
