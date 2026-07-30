using AGXUnity;
using UnityEditor;
using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// 速度倍率が実際に出ているかを Play 中に確認するウィンドウ。
    /// 指定した機体の移動速度を sim 時間基準と実時間基準の両方で出すので、
    /// 「指令どおり sim 時間では出ているが sim 時間の進みが遅い」のか、
    /// 「指令自体が効いていない」のかを切り分けられる。Editor 専用。
    /// </summary>
    public class SimSpeedDiagnosticsWindow : EditorWindow
    {
        [MenuItem("Tools/Sim Speed 診断")]
        private static void Open()
        {
            GetWindow<SimSpeedDiagnosticsWindow>("Sim Speed 診断");
        }

        private const float SampleIntervalSec = 1.0f;

        private string m_machineName = "zx200";
        private Transform m_machine;

        // 直近サンプル
        private float m_prevReal;
        private float m_prevSim;
        private int m_prevFixedCount;
        private Vector3 m_prevPos;
        private bool m_hasSample;

        // 計測結果
        private float m_ratio;            // sim 時間 / 実時間
        private float m_fixedPerRealSec;  // 実時間1秒あたりの FixedUpdate 回数
        private float m_speedSim;         // 機体速度 (m / sim 秒)
        private float m_speedReal;        // 機体速度 (m / 実秒)

        private int m_fixedCount;

        private void OnEnable()
        {
            EditorApplication.update += Sample;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Sample;
        }

        // FixedUpdate 回数は fixedTime の進みから求める（Editor 側で FixedUpdate は拾えない）
        private void Sample()
        {
            if (!EditorApplication.isPlaying)
            {
                m_hasSample = false;
                return;
            }

            m_fixedCount = Time.fixedDeltaTime > 0f ? Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime) : 0;

            float realNow = Time.realtimeSinceStartup;
            float simNow = Time.time;

            if (m_machine == null || m_machine.name != m_machineName)
            {
                var go = GameObject.Find(m_machineName);
                m_machine = go != null ? go.transform : null;
            }

            if (!m_hasSample)
            {
                m_prevReal = realNow;
                m_prevSim = simNow;
                m_prevFixedCount = m_fixedCount;
                m_prevPos = m_machine != null ? m_machine.position : Vector3.zero;
                m_hasSample = true;
                return;
            }

            float realElapsed = realNow - m_prevReal;
            if (realElapsed < SampleIntervalSec)
                return;

            float simElapsed = simNow - m_prevSim;
            m_ratio = simElapsed / Mathf.Max(realElapsed, 1e-6f);
            m_fixedPerRealSec = (m_fixedCount - m_prevFixedCount) / Mathf.Max(realElapsed, 1e-6f);

            if (m_machine != null)
            {
                float dist = Vector3.Distance(m_machine.position, m_prevPos);
                m_speedReal = dist / Mathf.Max(realElapsed, 1e-6f);
                m_speedSim = dist / Mathf.Max(simElapsed, 1e-6f);
                m_prevPos = m_machine.position;
            }

            m_prevReal = realNow;
            m_prevSim = simNow;
            m_prevFixedCount = m_fixedCount;

            Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play モードで使用します。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("設定値", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"選択倍率 (GlobalVariables): x{GlobalVariables.SimulationSpeedMultiplier:0.0}");
            EditorGUILayout.LabelField($"Time.timeScale: {Time.timeScale:0.00}");
            EditorGUILayout.LabelField($"Time.fixedDeltaTime: {Time.fixedDeltaTime:0.0000} s ({1f / Mathf.Max(Time.fixedDeltaTime, 1e-6f):0} Hz)");
            EditorGUILayout.LabelField($"Time.maximumDeltaTime: {Time.maximumDeltaTime:0.000} s");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AGX", EditorStyles.boldLabel);
            if (Simulation.HasInstance)
            {
                var sim = Simulation.Instance;
                EditorGUILayout.LabelField($"TimeStep: {sim.TimeStep:0.0000} s");
                EditorGUILayout.LabelField($"AutoSteppingMode: {sim.AutoSteppingMode}");

                var factor = sim.FixedUpdateRealTimeFactor;
                EditorGUILayout.LabelField($"FixedUpdateRealTimeFactor: {factor:0.00}");
                if (factor != 0f)
                    EditorGUILayout.HelpBox("0 以外だと AGX のステップが実時間ペースに制限され、倍率が出ません。", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("Simulation インスタンスなし");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("実測（直近1秒）", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"sim 時間 / 実時間: {m_ratio:0.00}");
            EditorGUILayout.LabelField($"達成率: {Achievement():0} %");
            EditorGUILayout.LabelField($"FixedUpdate: {m_fixedPerRealSec:0} 回 / 実秒（必要: {Required():0} 回）");
            EditorGUILayout.LabelField($"RealtimeFidelityProbe.LastRatio: {RealtimeFidelityProbe.LastRatio:0.00}");

            if (m_ratio > 0f && Achievement() < 90f)
                EditorGUILayout.HelpBox("指定倍率に対して sim 時間の進みが足りていません。物理計算が追いついていない状態です。", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("機体の移動速度", EditorStyles.boldLabel);
            m_machineName = EditorGUILayout.TextField("機体名", m_machineName);
            if (m_machine == null)
            {
                EditorGUILayout.HelpBox($"'{m_machineName}' が見つかりません。Hierarchy 上の名前を入れてください。", MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField($"sim 時間基準: {m_speedSim:0.000} m / sim 秒");
                EditorGUILayout.LabelField($"実時間基準: {m_speedReal:0.000} m / 実秒");
                EditorGUILayout.HelpBox(
                    "cmd_vel の指令値と一致するのは sim 時間基準の値です。実時間基準の値は sim 時間の進み方の影響を受けます。",
                    MessageType.None);
            }
        }

        private float Achievement()
        {
            float target = Mathf.Max(GlobalVariables.SimulationSpeedMultiplier, 0.01f);
            return m_ratio / target * 100f;
        }

        private float Required()
        {
            float target = Mathf.Max(GlobalVariables.SimulationSpeedMultiplier, 0.01f);
            return target / Mathf.Max(Time.fixedDeltaTime, 1e-6f);
        }
    }
}
