using UnityEngine;

namespace PWRISimulator
{
  /// <summary>
  /// Per-instance oriented-friction ContactMaterial registrar for a crawler
  /// bulldozer. All behaviour lives in
  /// <see cref="MachineContactMaterialRegistrar"/>; this concrete type only
  /// exists so a bulldozer prefab can wire its own asset references
  /// (BulldozerTrackShapeMat / BulldozerTrackFictionModel /
  /// BulldozerTrackVsGround / BulldozerTrackVsTerrain / tracks / body_link).
  ///
  /// NOTE: No placeable bulldozer machine prefab exists yet, so this component
  /// is currently unused. When a bulldozer machine is added, attach this and
  /// wire its fields (and set the corresponding ContactMaterialManager entries
  /// to IsOriented=false / ReferenceObject=null), mirroring the dump truck and
  /// excavator setup. See issue #18.
  /// </summary>
  [DisallowMultipleComponent]
  public class BulldozerContactMaterialRegistrar : MachineContactMaterialRegistrar
  {
  }
}
