using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// シミュレーション中の sim_time / real_time 比率を一定間隔でログ出力し、
    /// Time.timeScale による速度倍率設定が実時間で確保できているかを検証する補助コンポーネント。
    /// ratio は直近 ratioWindowSec 秒の移動窓で計算する(#75)。
    /// </summary>
    [DisallowMultipleComponent]
    public class RealtimeFidelityProbe : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float logIntervalSec = 1.0f;
        [SerializeField] bool logToConsole = true;
        [SerializeField, Min(1f)] float ratioWindowSec = 3.0f;
        [SerializeField, Min(0f)] float warnDelaySec = 3.0f;

        // 直近の sim_time / real_time 比率。ScoreBoard 等から参照される。
        public static float LastRatio = 1.0f;

        // (実時間, sim時間, フレーム番号) のサンプル列。logIntervalSec ごとに積み、
        // 窓内の最古サンプルとの差分で ratio と fps を出す
        readonly Queue<(float real, float sim, int frame)> samples = new Queue<(float real, float sim, int frame)>();
        float lastLogReal;
        float belowSinceReal = -1f;
        bool warned;

        void Start()
        {
            lastLogReal = Time.realtimeSinceStartup;
            samples.Enqueue((Time.realtimeSinceStartup, Time.time, Time.frameCount));
            LastRatio = GlobalVariables.SimulationSpeedMultiplier;
        }

        void Update()
        {
            float realNow = Time.realtimeSinceStartup;
            if (realNow - lastLogReal < logIntervalSec) return;
            lastLogReal = realNow;

            float simNow = Time.time;

            // 窓より古いサンプルを捨てる(差分の基準として最低1つは残す)
            while (samples.Count > 1 && realNow - samples.Peek().real > ratioWindowSec)
                samples.Dequeue();

            var oldest = samples.Peek();
            float realElapsed = realNow - oldest.real;
            float simElapsed = simNow - oldest.sim;
            float ratio = simElapsed / Mathf.Max(realElapsed, 1e-6f);
            float fps = (Time.frameCount - oldest.frame) / Mathf.Max(realElapsed, 1e-6f);
            // ratio を小数点第二位で四捨五入
            ratio = Mathf.Round(ratio * 100f) / 100f;
            LastRatio = ratio;
            samples.Enqueue((realNow, simNow, Time.frameCount));

            // 1.0 未満が warnDelaySec 続いたときだけ1回警告する(#34 / #75)。
            // 閾値ちょうど付近で 0.99↔1.01 と揺れるだけでは警告しない
            if (ratio < 1.0f)
            {
                if (belowSinceReal < 0f)
                    belowSinceReal = realNow;
                if (!warned && realNow - belowSinceReal >= warnDelaySec)
                {
                    warned = true;
                    Debug.LogWarning(
                        $"[RealtimeFidelity] ratio has stayed below 1.0 for {realNow - belowSinceReal:F1}s: " +
                        $"ratio={ratio:F2} target=x{GlobalVariables.SimulationSpeedMultiplier:0.0}");
                }
            }
            else
            {
                if (warned)
                    Debug.Log($"[RealtimeFidelity] ratio recovered to realtime: ratio={ratio:F2}");
                belowSinceReal = -1f;
                warned = false;
            }

            if (logToConsole)
            {
                Debug.Log(
                    $"[RealtimeFidelity] window sim={simElapsed:F2}s real={realElapsed:F2}s " +
                    $"ratio={ratio:F2} fps={fps:F1} target=x{GlobalVariables.SimulationSpeedMultiplier:0.0}");
            }
        }
    }
}
