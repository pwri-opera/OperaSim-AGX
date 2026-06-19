using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace PWRISimulator
{
    /// <summary>
    /// スコアリング表示更新処理
    /// </summary>
    public class Score : MonoBehaviour
    {
        public float timeOut = 500;
        private float timeElapsed;

        [SerializeField] float speedUpdateInterval = 0.5f;
        private float speedUpdateElapsed;

        private VisualElement root;

        void OnEnable()
        {
            root = this.GetComponent<UIDocument>().rootVisualElement;

            UpdateSpeedLabel();
        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            // Sim Speed / RT Perf は実時間で定期更新（スコア表示とは別系統）
            if (GlobalVariables.ActionMode == 3)
            {
                speedUpdateElapsed += Time.unscaledDeltaTime;
                if (speedUpdateElapsed >= speedUpdateInterval)
                {
                    UpdateSpeedLabel();
                    speedUpdateElapsed = 0.0f;
                }
            }

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= timeOut)
            {
                if (GlobalVariables.ActionMode == 3)
                {
                    var Score = root.Q<UnityEngine.UIElements.Label>("Value");
                    Score.text = CalcScore().ToString();
                }

                timeElapsed = 0.0f;

                //UnityEngine.Debug.Log("Call");
            }

            //GlobalVariables.incrementScore(10);

        }


        // SpeedValue ラベルの表示更新。RT Perf が 100 未満なら #Speed の背景をオレンジにする。
        private void UpdateSpeedLabel()
        {
            var speedLabel = root.Q<UnityEngine.UIElements.Label>("SpeedValue");
            if (speedLabel == null)
                return;

            int perf = Mathf.RoundToInt(RealtimeFidelityProbe.LastRatio * 100f);
            speedLabel.text =
                $"Sim Speed: {GlobalVariables.SimulationSpeedMultiplier:0.0} x\n" +
                $"RT Perf: {perf}%";

            var speedBox = root.Q<UnityEngine.UIElements.VisualElement>("Speed");
            if (speedBox != null)
                speedBox.style.backgroundColor = (perf < 100)
                    ? new Color(1f, 0.5f, 0f, 0.85f)
                    : new Color(250f / 255f, 249f / 255f, 249f / 255f, 0.59f);
        }


        private int CalcScore()
        {

            return GlobalVariables.score;
        }



    }
}
