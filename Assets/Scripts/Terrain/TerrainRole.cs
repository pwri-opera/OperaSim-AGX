using UnityEngine;
using AGXUnity.Model;

namespace PWRISimulator
{
    /// <summary>
    /// 放土地形と掘削地形を識別するためのマーカーコンポーネント。
    /// 各 DeformableTerrain に付与し、DumpSoil や TerrainScore が正しい地形を参照できるようにする。
    /// </summary>
    [RequireComponent(typeof(DeformableTerrain))]
    public class TerrainRole : MonoBehaviour
    {
        public enum Role
        {
            Excavation = 0,
            Dump = 1
        }

        [Tooltip("この地形の役割: Excavation=掘削用, Dump=放土用")]
        public Role role = Role.Excavation;

        /// <summary>
        /// 指定した Role を持つアクティブな DeformableTerrain を検索する。
        /// 見つからない場合は null を返す。
        /// </summary>
        public static DeformableTerrain FindTerrainByRole(Role targetRole)
        {
#if UNITY_6000_0_OR_NEWER
            var terrains = Object.FindObjectsByType<DeformableTerrain>(FindObjectsSortMode.None);
#else
            var terrains = Object.FindObjectsOfType<DeformableTerrain>();
#endif
            foreach (var terrain in terrains)
            {
                if (terrain == null || !terrain.isActiveAndEnabled)
                    continue;
                var role = terrain.GetComponent<TerrainRole>();
                if (role != null && role.role == targetRole)
                    return terrain;
            }
            return null;
        }
    }
}
