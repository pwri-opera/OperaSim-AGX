using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

namespace PWRISimulator
{
    public class PostStepProbe : MonoBehaviour {
    void OnEnable() { Simulation.Instance.StepCallbacks.PostStepForward += Tick; }
    void OnDisable(){ if (Simulation.HasInstance)
        Simulation.Instance.StepCallbacks.PostStepForward -= Tick; }
    void Tick(){ Debug.Log($"PostStep t={Simulation.Instance.Native.getTimeStamp():F3}"); }
    }
}
