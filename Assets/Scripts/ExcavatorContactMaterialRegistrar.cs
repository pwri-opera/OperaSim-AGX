using UnityEngine;

namespace PWRISimulator
{
  /// <summary>
  /// Per-instance oriented-friction ContactMaterial registrar for the crawler
  /// excavator (zx200). All behaviour lives in
  /// <see cref="MachineContactMaterialRegistrar"/>; this concrete type only
  /// exists so the excavator prefab can wire its own asset references
  /// (ExcavatorTrackShapeMat / ExcavatorTrackFictionModel /
  /// ExcavatorTrackVsGround / ExcavatorTrackVsTerrain / tracks / body_link).
  /// </summary>
  [DisallowMultipleComponent]
  public class ExcavatorContactMaterialRegistrar : MachineContactMaterialRegistrar
  {
  }
}
