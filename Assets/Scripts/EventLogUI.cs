using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PWRISimulator
{
    /// <summary>
    /// Top-left HUD that shows the most recent score events (additions /
    /// deductions) as they happen. Subscribes to
    /// <see cref="GlobalVariables.OnScoreEvent"/> and keeps the latest
    /// <see cref="MaxEntries"/> rows; newer at the top, older pushed down
    /// until evicted.
    /// </summary>
    public class EventLogUI : MonoBehaviour
    {
        private const int MaxEntries = 5;

        private static readonly Color PositiveColor = new Color(0.55f, 1f, 0.55f);
        private static readonly Color NegativeColor = new Color(1f, 0.55f, 0.55f);

        private readonly LinkedList<(string text, Color color)> _entries
            = new LinkedList<(string, Color)>();
        private Label[] _labels;

        private void OnEnable()
        {
            GlobalVariables.OnScoreEvent += HandleEvent;
        }

        private void OnDisable()
        {
            GlobalVariables.OnScoreEvent -= HandleEvent;
        }

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) return;
            var root = doc.rootVisualElement;
            _labels = new Label[MaxEntries];
            for (int i = 0; i < MaxEntries; i++)
            {
                _labels[i] = root.Q<Label>($"Entry{i}");
            }
            Refresh();
        }

        private void HandleEvent(GlobalVariables.ScoreEvent evt)
        {
            string name = ScoreEventLogger.GetName(evt.Id);
            string time = FormatElapsed(GlobalVariables.GameTime - CountdownTimer.timeRemaining);
            string text = string.Format("[{0}] {1} {2} {3:+0;-0}pt", time, evt.Id, name, evt.Point);
            Color color = evt.Point >= 0 ? PositiveColor : NegativeColor;

            _entries.AddFirst((text, color));
            while (_entries.Count > MaxEntries)
                _entries.RemoveLast();

            Refresh();
        }

        // Sim 開始からの経過時間を mm:ss 形式で返す。イベント発生時刻として併記する。
        private static string FormatElapsed(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, secs);
        }

        private void Refresh()
        {
            if (_labels == null) return;

            var node = _entries.First;
            for (int i = 0; i < _labels.Length; i++)
            {
                if (_labels[i] == null) continue;
                if (node != null)
                {
                    _labels[i].text = node.Value.text;
                    _labels[i].style.color = node.Value.color;
                    node = node.Next;
                }
                else
                {
                    _labels[i].text = string.Empty;
                }
            }
        }
    }
}
