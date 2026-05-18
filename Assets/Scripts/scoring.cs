using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace PWRISimulator
{
    /// <summary>
    /// �X�R�A�����O�\���X�V����
    /// </summary>
    public class Score : MonoBehaviour
    {
        public float timeOut = 500;
        private float timeElapsed;

        private VisualElement root;

        void OnEnable()
        {
            root = this.GetComponent<UIDocument>().rootVisualElement;

            var speedLabel = root.Q<UnityEngine.UIElements.Label>("SpeedValue");
            if (speedLabel != null)
                speedLabel.text = $"x{GlobalVariables.SimulationSpeedMultiplier:0.0}";
        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= timeOut)
            {
                if (GlobalVariables.ActionMode == 3)
                {
                    var Score = root.Q<UnityEngine.UIElements.Label>("Value");
                    Score.text = CalcScore().ToString();

                    var speedLabel = root.Q<UnityEngine.UIElements.Label>("SpeedValue");
                    if (speedLabel != null)
                    {
                        speedLabel.text =
                            $"x{GlobalVariables.SimulationSpeedMultiplier:0.0} " +
                            $"(実x{RealtimeFidelityProbe.LastRatio:0.0})";
                    }
                }

                timeElapsed = 0.0f;

                //UnityEngine.Debug.Log("Call");
            }

            //GlobalVariables.incrementScore(10);

        }


        private int CalcScore()
        {

            return GlobalVariables.score;
        }



    }
}
