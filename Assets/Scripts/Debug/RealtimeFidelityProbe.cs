using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// シミュレーション中の sim_time / real_time 比率を一定間隔でログ出力し、
    /// Time.timeScale による速度倍率設定が実時間で確保できているかを検証する補助コンポーネント。
    /// </summary>
    public class RealtimeFidelityProbe : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float logIntervalSec = 1.0f;
        [SerializeField] bool logToConsole = true;

        // 直近の sim_time / real_time 比率。ScoreBoard 等から参照される。
        public static float LastRatio = 1.0f;

        float realStartTime;
        float simStartTime;
        float lastLogReal;

        void Start()
        {
            realStartTime = Time.realtimeSinceStartup;
            simStartTime = Time.time;
            lastLogReal = realStartTime;
            LastRatio = GlobalVariables.SimulationSpeedMultiplier;
        }

        void Update()
        {
            float realNow = Time.realtimeSinceStartup;
            if (realNow - lastLogReal < logIntervalSec) return;

            float realElapsed = realNow - realStartTime;
            float simElapsed = Time.time - simStartTime;
            float ratio = simElapsed / Mathf.Max(realElapsed, 1e-6f);
            LastRatio = ratio;

            if (logToConsole)
            {
                Debug.Log(
                    $"[RealtimeFidelity] sim={simElapsed:F2}s real={realElapsed:F2}s " +
                    $"ratio={ratio:F2} target=x{GlobalVariables.SimulationSpeedMultiplier:0.0}");
            }

            lastLogReal = realNow;
        }
    }
}
