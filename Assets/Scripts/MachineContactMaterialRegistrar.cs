using System.Collections.Generic;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using UnityEngine;

namespace PWRISimulator
{
  /// <summary>
  /// Per-instance registrar for oriented (anisotropic) friction ContactMaterials
  /// on a tracked construction machine. Clones the base ShapeMaterial /
  /// FrictionModel / ContactMaterials at runtime so each vehicle instance owns a
  /// unique (TrackShapeMaterial, Ground-or-Terrain) pair whose friction reference
  /// frame is bound to this vehicle's chassis.
  ///
  /// Why per-instance clones are required:
  /// - AGX stores at most one ContactMaterial per (ShapeMaterial_A, ShapeMaterial_B) pair;
  ///   a second explicit registration for the same pair is rejected.
  /// - Oriented-friction reference frame is baked into FrictionModel at native creation,
  ///   so a shared FrictionModel cannot serve multiple vehicles with different headings.
  /// Cloning the ShapeMaterial per vehicle makes the pair distinct, and cloning the
  /// FrictionModel avoids cross-instance mutation of the shared asset's native.
  ///
  /// Attach this directly to a machine prefab (dump truck, excavator, ...) and
  /// wire its asset references in the inspector.
  /// </summary>
  /// <remarks>
  /// 運用上の注意
  ///
  /// 履帯の oriented friction はこのクラスが担当する。
  /// AGXUnity 標準の ContactMaterialManager では設定しないこと。
  /// Manager のエントリに Reference Object を設定すると #10 が再発する。
  ///
  /// 適用先は ic120 / zx120 / zx200 の3プレハブ。
  /// ブルドーザは未適用で、ワールド軸基準の異方性になっている (#84)。
  ///
  /// 落とし穴
  /// - Manager のエントリは Is Oriented を off のままにする
  /// - プレハブ編集でインスペクタ参照が外れることがある。
  ///   開始時に LogError を出す (#67)。自動復元はしない
  /// - 異方性を効かせたくない CM (履帯 vs 転輪) は
  ///   baseNonOrientedContactMaterials に入れる (#59)
  /// - Registrar を持たない機体は base CM にフォールバックし、
  ///   ワールド軸基準になる。機体の向きには追従しない
  ///
  /// 新しい機体に適用するとき
  /// 1. 機体プレハブのルートにこのコンポーネントを付ける
  /// 2. Base Track Shape Material と Base Friction Model を設定
  /// 3. Base Contact Materials に履帯 vs Ground/Terrain の CM、
  ///    Base Non Oriented Contact Materials に履帯 vs 転輪の CM
  /// 4. Track Components に TrackL / TrackR
  /// 5. Reference Object に body_link、Primary Direction に前方軸
  /// 6. Manager 側の対応エントリは Is Oriented を off のまま
  ///
  /// 詳細は業務報告書 付録 B に記載した。
  /// </remarks>
  [DisallowMultipleComponent]
  public class MachineContactMaterialRegistrar : MonoBehaviour
  {
    [Tooltip("Base ShapeMaterial used by track box shapes and Track components. Cloned per instance.")]
    public ShapeMaterial baseTrackShapeMaterial;

    [Tooltip("Base FrictionModel referenced by the track ContactMaterials. Cloned per instance to avoid side-effects on the shared asset.")]
    public FrictionModel baseFrictionModel;

    [Tooltip("Base ContactMaterials whose Material1 is the track ShapeMaterial. Each is cloned and rebound to the cloned ShapeMaterial + cloned FrictionModel.")]
    public ContactMaterial[] baseContactMaterials;

    [Tooltip("Base ContactMaterials whose Material1 is the track ShapeMaterial but which must NOT use oriented friction (e.g. track vs wheel). Each is cloned and rebound to the cloned ShapeMaterial, keeping its own FrictionModel.")]
    public ContactMaterial[] baseNonOrientedContactMaterials;

    [Tooltip("AGXUnity Track components (e.g. TrackL / TrackR) whose Material field feeds runtime-spawned shoes.")]
    public Track[] trackComponents;

    [Tooltip("GameObject supplying the reference frame for oriented friction. Must have RigidBody, Collide.Shape, or ObserverFrame. Typically the chassis rigid body (body_link).")]
    public GameObject referenceObject;

    [Tooltip("Primary (longitudinal) axis of the oriented friction frame, in the reference object's local coordinates.")]
    public FrictionModel.PrimaryDirection primaryDirection = FrictionModel.PrimaryDirection.X;

    /// <summary>
    /// Appended to every diagnostic so that whoever reads the Console learns where
    /// track friction is configured. AGXUnity's manual points at ContactMaterialManager,
    /// which is not where this project sets it up.
    /// </summary>
    private const string RegistrarNote =
      "Note: track friction is registered per machine instance by this component, not by AGXUnity's " +
      "ContactMaterialManager. Do not set a Reference Object on the manager entry.";

    private ShapeMaterial m_clonedShapeMaterial;
    private FrictionModel m_clonedFrictionModel;
    private readonly List<ContactMaterial> m_clonedContactMaterials = new List<ContactMaterial>();
    private readonly List<ContactMaterial> m_clonedNonOrientedContactMaterials = new List<ContactMaterial>();

    private void Awake()
    {
      if ( baseTrackShapeMaterial == null || baseFrictionModel == null ||
           baseContactMaterials == null || baseContactMaterials.Length == 0 ||
           referenceObject == null ) {
        Debug.LogError( $"{GetType().Name} ({name}): required inspector fields are missing, so per-instance registration is skipped. " +
                        "This machine's tracks fall back to the base ContactMaterial, whose anisotropic friction follows the world axes " +
                        "instead of the machine heading. Assign Base Track Shape Material, Base Friction Model, Base Contact Materials " +
                        $"and Reference Object on this component. {RegistrarNote}", this );
        return;
      }

      ValidateReferences();

      m_clonedShapeMaterial = Instantiate( baseTrackShapeMaterial );
      m_clonedShapeMaterial.name = baseTrackShapeMaterial.name + "_" + GetInstanceID();

      foreach ( var shape in GetComponentsInChildren<Shape>( true ) ) {
        if ( shape.Material == baseTrackShapeMaterial )
          shape.Material = m_clonedShapeMaterial;
      }

      if ( trackComponents != null ) {
        foreach ( var track in trackComponents ) {
          if ( track != null && track.Material == baseTrackShapeMaterial )
            track.Material = m_clonedShapeMaterial;
        }
      }

      m_clonedFrictionModel = Instantiate( baseFrictionModel );
      m_clonedFrictionModel.name = baseFrictionModel.name + "_" + GetInstanceID();

      foreach ( var baseCM in baseContactMaterials ) {
        if ( baseCM == null ) continue;
        var clone = Instantiate( baseCM );
        clone.name = baseCM.name + "_" + GetInstanceID();
        // Material2 intentionally left as-is (Ground / Terrain counterpart); only the track side is swapped.
        clone.Material1 = m_clonedShapeMaterial;
        clone.FrictionModel = m_clonedFrictionModel;
        m_clonedContactMaterials.Add( clone );
      }

      if ( baseNonOrientedContactMaterials != null ) {
        foreach ( var baseCM in baseNonOrientedContactMaterials ) {
          if ( baseCM == null ) continue;
          var clone = Instantiate( baseCM );
          clone.name = baseCM.name + "_" + GetInstanceID();
          // FrictionModel intentionally left as-is: these pairs (e.g. track vs wheel) must not become oriented.
          clone.Material1 = m_clonedShapeMaterial;
          m_clonedNonOrientedContactMaterials.Add( clone );
        }
      }
    }

    /// <summary>
    /// Detects broken inspector references that the required-fields guard does not
    /// cover. Emits LogError only; registration continues either way so
    /// the behavior stays the same as before the checks were added.
    /// </summary>
    private void ValidateReferences()
    {
      foreach ( var baseCM in baseContactMaterials ) {
        if ( baseCM == null )
          Debug.LogError( $"{GetType().Name} ({name}): Base Contact Materials contains a null element, so that entry is skipped. " +
                          "The corresponding track vs ground/terrain pair keeps the base ContactMaterial and loses its per-instance " +
                          "oriented friction. Re-assign the missing asset on this component; editing the machine prefab is the usual " +
                          $"way these references break. {RegistrarNote}", this );
      }

      if ( baseNonOrientedContactMaterials != null ) {
        foreach ( var baseCM in baseNonOrientedContactMaterials ) {
          if ( baseCM == null )
            Debug.LogError( $"{GetType().Name} ({name}): Base Non Oriented Contact Materials contains a null element, so that entry is skipped. " +
                            "The corresponding pair (track vs wheel) keeps the base ContactMaterial instead of the per-instance clone. " +
                            $"Re-assign the missing asset on this component. {RegistrarNote}", this );
        }
      }

      if ( trackComponents == null || trackComponents.Length == 0 ) {
        Debug.LogError( $"{GetType().Name} ({name}): Track Components is empty, so runtime-spawned track shoes keep the base ShapeMaterial " +
                        "and stay outside this machine's oriented friction. Assign the AGXUnity Track components (TrackL / TrackR) " +
                        $"on this component. {RegistrarNote}", this );
      }
      else {
        foreach ( var track in trackComponents ) {
          if ( track == null )
            Debug.LogError( $"{GetType().Name} ({name}): Track Components contains a null element, so that track's shoes keep the base " +
                            "ShapeMaterial and stay outside this machine's oriented friction. Re-assign the missing Track component " +
                            $"on this component. {RegistrarNote}", this );
        }
      }

      if ( referenceObject.GetComponent<RigidBody>() == null &&
           referenceObject.GetComponent<Shape>() == null &&
           referenceObject.GetComponent<ObserverFrame>() == null )
        Debug.LogError( $"{GetType().Name} ({name}): Reference Object '{referenceObject.name}' has no RigidBody, Shape or ObserverFrame, " +
                        "so the oriented friction reference frame cannot be resolved and the tracks fall back to world-axis friction. " +
                        $"Assign the chassis rigid body (body_link) to Reference Object on this component. {RegistrarNote}", this );
    }

    private void Start()
    {
      if ( m_clonedContactMaterials.Count == 0 && m_clonedNonOrientedContactMaterials.Count == 0 )
        return;

      var simulation = Simulation.Instance;
      if ( simulation == null || simulation.Native == null ) {
        Debug.LogError( $"{GetType().Name} ({name}): the AGX Simulation is not initialized, so the cloned ContactMaterials cannot be " +
                        "registered and this machine's tracks use the base ContactMaterial. This usually means the machine was spawned " +
                        $"before AGXUnity's Simulation was created. {RegistrarNote}", this );
        return;
      }

      var manager = ContactMaterialManager.HasInstance ? ContactMaterialManager.Instance : null;
      var nativeManager = simulation.Native.getMaterialManager();

      // Mirrors ContactMaterialManager.Initialize(entry): create native, bind oriented friction, add explicit.
      // GetInitialized initializes the clone in place and returns it (or null on failure),
      // so using clone below is safe once initialized is non-null (#123).
      foreach ( var clone in m_clonedContactMaterials ) {
        var initialized = clone.GetInitialized<ContactMaterial>();
        if ( initialized == null )
          continue;

        clone.InitializeOrientedFriction( true, referenceObject, primaryDirection );
        nativeManager.add( clone.Native );

        if ( manager != null )
          manager.Add( clone );
      }

      foreach ( var clone in m_clonedNonOrientedContactMaterials ) {
        var initialized = clone.GetInitialized<ContactMaterial>();
        if ( initialized == null )
          continue;

        nativeManager.add( clone.Native );

        if ( manager != null )
          manager.Add( clone );
      }
    }

    private void OnDestroy()
    {
      var manager = ContactMaterialManager.HasInstance ? ContactMaterialManager.Instance : null;

      foreach ( var clone in m_clonedContactMaterials ) {
        if ( clone == null ) continue;
        if ( manager != null )
          manager.Remove( clone );
        clone.Destroy();
        Destroy( clone );
      }
      m_clonedContactMaterials.Clear();

      foreach ( var clone in m_clonedNonOrientedContactMaterials ) {
        if ( clone == null ) continue;
        if ( manager != null )
          manager.Remove( clone );
        clone.Destroy();
        Destroy( clone );
      }
      m_clonedNonOrientedContactMaterials.Clear();

      if ( m_clonedFrictionModel != null ) {
        m_clonedFrictionModel.Destroy();
        Destroy( m_clonedFrictionModel );
        m_clonedFrictionModel = null;
      }

      if ( m_clonedShapeMaterial != null ) {
        m_clonedShapeMaterial.Destroy();
        Destroy( m_clonedShapeMaterial );
        m_clonedShapeMaterial = null;
      }
    }
  }
}
