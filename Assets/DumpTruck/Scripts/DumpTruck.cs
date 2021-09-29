using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    public class DumpTruck : ConstructionMachine
    {
        [Header("Constraint Controls")]
        public ConstraintControl leftSprocket;
        public ConstraintControl rightSprocket;
        public ConstraintControl containerTilt;
        
        protected override bool Initialize()
        {
            bool success = base.Initialize();

            RegisterConstraintControl(leftSprocket);
            RegisterConstraintControl(rightSprocket);
            RegisterConstraintControl(containerTilt);

            return success;
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(DumpTruck))]
    public class DumpTruckEditor : ConstructionMachineEditor
    {
        public override void OnInspectorGUI()
        {
            // ConstructionMachineEditor‚ÌGUI‚ð•\Ž¦
            base.OnInspectorGUI();
        }
    }
#endif
}
