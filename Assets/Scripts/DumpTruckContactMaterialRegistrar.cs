using UnityEngine;

namespace PWRISimulator
{
  /// <summary>
  /// Per-instance oriented-friction ContactMaterial registrar for the crawler
  /// dump truck (ic120). All behaviour lives in
  /// <see cref="MachineContactMaterialRegistrar"/>; this concrete type only
  /// exists so the dump-truck prefab can wire its own asset references and so
  /// the existing prefab component reference (script GUID) stays valid.
  /// </summary>
  [DisallowMultipleComponent]
  public class DumpTruckContactMaterialRegistrar : MachineContactMaterialRegistrar
  {
  }
}
