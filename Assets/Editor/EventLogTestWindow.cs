using System.IO;
using UnityEditor;
using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// イベントログ表示(#3)の確認用ウィンドウ。ダミーのスコアイベントを発火して、
    /// HUD とログファイルへの反映を Play モードで確認する。Editor 専用なのでビルドには含まれない。
    ///
    /// 確認の流れ:
    ///   1. Play 直後(ActionMode != 3)に発火 → HUD に何も出ないこと
    ///   2. Start Simulation 後に発火 → 新しい順に最大5件表示されること
    ///   3. Reset 押下 → 表示が消えること
    /// </summary>
    public class EventLogTestWindow : EditorWindow
    {
        private static readonly (GlobalVariables.ScoreEventId id, int point)[] Events =
        {
            (GlobalVariables.ScoreEventId.P01, 3),
            (GlobalVariables.ScoreEventId.P02, 5),
            (GlobalVariables.ScoreEventId.P03, 50),
            (GlobalVariables.ScoreEventId.M01, -2),
            (GlobalVariables.ScoreEventId.M02, -5),
            (GlobalVariables.ScoreEventId.M03, -1),
            (GlobalVariables.ScoreEventId.M04, -3),
        };

        private static string LogPath =>
            Path.Combine(GlobalVariables.BACKUP_FOLDER, "score_events.log");

        [MenuItem("Tools/EventLog Test")]
        private static void Open()
        {
            GetWindow<EventLogTestWindow>("EventLog Test");
        }

        // Play 中は ActionMode と score が動くので再描画し続ける
        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play モードで使用します。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"ActionMode: {ActionModeText()}");
            EditorGUILayout.LabelField($"score: {GlobalVariables.score}");
            if (GlobalVariables.ActionMode != 3)
                EditorGUILayout.HelpBox("シミュレーション中(ActionMode 3)以外は HUD とログに出ないのが正しい挙動です。",
                                        MessageType.None);

            EditorGUILayout.Space();

            foreach (var (id, point) in Events)
            {
                if (GUILayout.Button($"{id} {ScoreEventLogger.GetName(id)} {point:+0;-0}pt"))
                    Fire(id, point);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("まとめて発火(7件)"))
            {
                foreach (var (id, point) in Events)
                    Fire(id, point);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(LogPath, EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!File.Exists(LogPath)))
                {
                    if (GUILayout.Button("ログを開く"))
                        EditorUtility.OpenWithDefaultApp(LogPath);

                    if (GUILayout.Button("ログを削除"))
                        AssetDatabase.DeleteAsset(LogPath);
                }
            }
        }

        private static string ActionModeText()
        {
            switch (GlobalVariables.ActionMode)
            {
                case 0: return "0 (truck placement)";
                case 1: return "1 (camera placement)";
                case 2: return "2 (camera selection)";
                case 3: return "3 (simulation)";
                default: return $"{GlobalVariables.ActionMode} (menu)";
            }
        }

        private static void Fire(GlobalVariables.ScoreEventId id, int point)
        {
            GlobalVariables.RegisterScoreEvent(
                new GlobalVariables.ScoreEvent { Id = id, Point = point });
            Debug.Log($"[EventLogTest] fired {id} {point:+0;-0}pt (ActionMode={GlobalVariables.ActionMode})");
        }
    }
}
