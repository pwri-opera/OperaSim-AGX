using AGXUnity.Utils;
using UnityEngine;

namespace AGXUnity.Model
{
  [AddComponentMenu( "" )]
  [HideInInspector]
  public class DeformableTerrainConnector : MonoBehaviour
  {
    public float[,] InitialHeights { get; private set; } = null;
    public float MaximumDepth { get; set; } = float.NaN;

#if UNITY_EDITOR
    private TerrainData m_sourceTerrainData = null;
    private TerrainData m_runtimeTerrainData = null;
#endif

    public Terrain Terrain { get => GetComponent<Terrain>(); }

    public Vector3 GetOffsetPosition()
    {
      if ( InitialHeights == null )
        return transform.position + MaximumDepth * Vector3.down;
      else
        return transform.position;
    }

    public float[,] WriteTerrainDataOffset()
    {
      if ( float.IsNaN( MaximumDepth ) ) {
        Debug.LogError( "Writing terrain offset without first setting depth!" );
        MaximumDepth = 0;
      }
#if UNITY_EDITOR
      UseRuntimeTerrainDataCopy();
#endif
      var resolution = TerrainUtils.TerrainDataResolution(Terrain.terrainData);
      InitialHeights = Terrain.terrainData.GetHeights( 0, 0, resolution, resolution );
      transform.position += MaximumDepth * Vector3.down;
      return TerrainUtils.WriteTerrainDataOffsetRaw( Terrain, MaximumDepth );
    }

#if UNITY_EDITOR
    private void UseRuntimeTerrainDataCopy()
    {
      if ( m_runtimeTerrainData != null )
        return;

      var sourceTerrainData = Terrain.terrainData;
      if ( sourceTerrainData == null || !UnityEditor.AssetDatabase.Contains( sourceTerrainData ) )
        return;

      m_sourceTerrainData = sourceTerrainData;
      m_runtimeTerrainData = UnityEngine.Object.Instantiate( sourceTerrainData );
      m_runtimeTerrainData.name = sourceTerrainData.name + " (Runtime)";
      m_runtimeTerrainData.hideFlags = HideFlags.DontSave;

      Terrain.terrainData = m_runtimeTerrainData;
      var terrainCollider = GetComponent<TerrainCollider>();
      if ( terrainCollider != null )
        terrainCollider.terrainData = m_runtimeTerrainData;
    }
#endif

    private void OnDestroy()
    {
      if ( InitialHeights != null ) {
        transform.position += MaximumDepth * Vector3.up;
        Terrain.terrainData.SetHeights( 0, 0, InitialHeights );

#if UNITY_EDITOR
        if ( m_runtimeTerrainData == null ) {
          // If the editor is closed during play the modified height
          // data isn't saved, this resolves corrupt heights in such case.
          UnityEditor.EditorUtility.SetDirty( Terrain.terrainData );
          UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
      }

#if UNITY_EDITOR
      if ( m_runtimeTerrainData != null ) {
        Terrain.terrainData = m_sourceTerrainData;
        var terrainCollider = GetComponent<TerrainCollider>();
        if ( terrainCollider != null )
          terrainCollider.terrainData = m_sourceTerrainData;

        UnityEngine.Object.DestroyImmediate( m_runtimeTerrainData );
        m_runtimeTerrainData = null;
        m_sourceTerrainData = null;
      }
#endif
    }
  }
}
