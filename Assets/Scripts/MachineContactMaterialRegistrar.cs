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

    private ShapeMaterial m_clonedShapeMaterial;
    private FrictionModel m_clonedFrictionModel;
    private readonly List<ContactMaterial> m_clonedContactMaterials = new List<ContactMaterial>();
    private readonly List<ContactMaterial> m_clonedNonOrientedContactMaterials = new List<ContactMaterial>();

    private void Awake()
    {
      if ( baseTrackShapeMaterial == null || baseFrictionModel == null ||
           baseContactMaterials == null || baseContactMaterials.Length == 0 ||
           referenceObject == null ) {
        Debug.LogError( $"{GetType().Name}: required fields missing, skipping per-instance registration.", this );
        return;
      }

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

    private void Start()
    {
      if ( m_clonedContactMaterials.Count == 0 && m_clonedNonOrientedContactMaterials.Count == 0 )
        return;

      var simulation = Simulation.Instance;
      if ( simulation == null || simulation.Native == null ) {
        Debug.LogError( $"{GetType().Name}: AGX Simulation not initialized; cannot register ContactMaterials.", this );
        return;
      }

      var manager = ContactMaterialManager.HasInstance ? ContactMaterialManager.Instance : null;
      var nativeManager = simulation.Native.getMaterialManager();

      // Mirrors ContactMaterialManager.Initialize(entry): create native, bind oriented friction, add explicit.
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
