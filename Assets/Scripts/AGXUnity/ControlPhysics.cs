using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AGXUnity;
using AGXUnity.Utils;
using agx;


namespace PWRISimulator
{
    public class ControlPhysics : MonoBehaviour
    {

        private Simulation simulation;
        private bool PhysicsFlg = false;

        void Awake()
        {

        }


        // Start is called before the first frame update
        void Start()
        {

            simulation = FindObjectOfType<Simulation>();

            if (simulation != null)
            {
                //simulation.AutoSteppingMode = Simulation.AutoSteppingModes.Disabled;
                if (PhysicsFlg == false)
                {
                    PausePhysics();
                }
                else
                {
                    ResumePhysics();
                }
            }

        }

        // Update is called once per frame
        void Update()
        {


            //if (Input.GetKeyUp(KeyCode.S))
            if(GlobalVariables.ActionMode == 3  && PhysicsFlg == false)
            {
                //if (PhysicsFlg == false)
                //{
                    ResumePhysics();
                    PhysicsFlg = true;
                    UnityEngine.Debug.Log("Physics Start!");
                //}
                //else
                //{
                //    PausePhysics();
                //    PhysicsFlg = false;

                //}

            }
            //else if (GlobalVariables.ActionMode == -1 && PhysicsFlg == true)
            else if (GlobalVariables.ActionMode == -1 && PhysicsFlg ==true && GlobalVariables.SelectMode == -1) // 地形の初期化が完了したら停止 
            {
                PausePhysics();
                PhysicsFlg = false;
                UnityEngine.Debug.Log("Physics Stop!");
            }


            }


            void ResumePhysics()
        {
            simulation.AutoSteppingMode = Simulation.AutoSteppingModes.FixedUpdate;
        }

        void PausePhysics()
        {
            simulation.AutoSteppingMode = Simulation.AutoSteppingModes.Disabled;
        }


    }
}
