using UnityEngine;

namespace PWRISimulator
{
  /// <summary>
  /// Compatibility subclass for the crawler dump truck (ic120). All behaviour
  /// lives in <see cref="MachineContactMaterialRegistrar"/>; this type only
  /// exists so the existing ic120 prefab's component reference (script GUID)
  /// stays valid. New machines should attach MachineContactMaterialRegistrar
  /// directly instead of adding a per-machine subclass.
  /// </summary>
  [DisallowMultipleComponent]
  public class DumpTruckContactMaterialRegistrar : MachineContactMaterialRegistrar
  {
  }
}
