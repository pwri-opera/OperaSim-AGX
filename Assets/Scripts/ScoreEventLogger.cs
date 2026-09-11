using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// Persists every score event produced by <see cref="GlobalVariables.OnScoreEvent"/>
    /// to a text log on disk. Subscribed once per process via
    /// <c>RuntimeInitializeOnLoadMethod</c> so the writer survives scene
    /// reloads (Load / Reset) and captures events from the moment play begins.
    /// </summary>
    public static class ScoreEventLogger
    {
        private const string LogFileName = "score_events.log";
        private static bool _subscribed = false;

        private static readonly Dictionary<GlobalVariables.ScoreEventId, string> IdToName
            = new Dictionary<GlobalVariables.ScoreEventId, string>
        {
            { GlobalVariables.ScoreEventId.P01, "掘削" },
            { GlobalVariables.ScoreEventId.P02, "土砂積込み" },
            { GlobalVariables.ScoreEventId.P03, "土砂積降ろし" },
            { GlobalVariables.ScoreEventId.M01, "地形変形" },
            { GlobalVariables.ScoreEventId.M02, "衝突" },
            { GlobalVariables.ScoreEventId.M03, "コースアウト" },
            { GlobalVariables.ScoreEventId.M04, "コースラップ" },
        };

        public static string GetName(GlobalVariables.ScoreEventId id)
            => IdToName.TryGetValue(id, out var name) ? name : id.ToString();

        public static string FormatLine(GlobalVariables.ScoreEvent evt, float remaining)
        {
            return string.Format(
                "[{0:F1}s remaining] {1} {2} {3:+0;-0}pt",
                remaining, evt.Id, GetName(evt.Id), evt.Point);
        }

        private static string LogPath =>
            Path.Combine(GlobalVariables.BACKUP_FOLDER, LogFileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (_subscribed) return;
            _subscribed = true;
            GlobalVariables.OnScoreEvent += HandleEvent;
        }

        private static void HandleEvent(GlobalVariables.ScoreEvent evt)
        {
            float remaining;
            try { remaining = CountdownTimer.timeRemaining; }
            catch { remaining = 0f; }

            string line = FormatLine(evt, remaining);

            try
            {
                if (!Directory.Exists(GlobalVariables.BACKUP_FOLDER))
                    Directory.CreateDirectory(GlobalVariables.BACKUP_FOLDER);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ScoreEventLogger: failed to write log: {e.Message}");
            }
        }
    }
}
