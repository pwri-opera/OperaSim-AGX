// Copyright 2021 VMC Motion Technologies Co., Ltd.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AGXUnity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VMT.Profiling
{
    public class PerformanceMeasure : MonoBehaviour
    {
        public class TimeSeries
        {
            public double average { get; private set; }
            public double median { get; private set; }
            public double max { get; private set; }
            public double min { get; private set; }

            List<float> times;
            int? initialCapacity;
            int maxSize = 131072;

            public TimeSeries(int? capacity)
            {
                if(capacity.HasValue && capacity > maxSize)
                {
                    Debug.LogWarning($"PerformanceMeasure : TimeSeries capacity ({capacity}) is greater than max " +
                                     $"size {maxSize}. Will clamp capacity to max size.");
                    capacity = maxSize;
                }
                initialCapacity = capacity;
                Clear();
            }

            public void Clear()
            {
                times = initialCapacity != null ? new List<float>(initialCapacity.Value) :
                                                  new List<float>();
                average = median = max = 0;
                min = 999;
            }

            public void AddTime(float time)
            {
                if (times.Count >= maxSize)
                {
                    Debug.LogError($"PerformanceMeasure : Cannot add value to TimeSeries because max size has been " +
                                   $"reached ({maxSize}).");
                    return;
                }
                times.Add(time);
            }

            public void ComputeResult()
            {
                double total = 0;
                foreach (var t in times)
                {
                    total += t;
                    max = System.Math.Max(t, max);
                    min = System.Math.Min(t, min);
                }
                if (times.Count > 0)
                {
                    average = total / times.Count;
                    times.Sort();
                    median = times[times.Count / 2];
                }
                else
                {
                    average = 0;
                    median = 0;
                }
            }
        }

        public bool autoStop = false;

        [PWRISimulator.ConditionalHide(nameof(autoStop), hideCompletely = true)] // TODO
        public double timeUntilStop = 10.0;

        public TimeSeries unscaledDeltaTimeSeries { private set; get; } = null;

        public TimeSeries agxStepForwardSeries { private set; get; } = null;

        public bool isMeasuring { private set; get; } = false;

        public double timeMeasured { private set; get; } = 0.0;

        double measureStartTime = 0.0;
        
        void Update()
        {
            if (isMeasuring)
            {
                if (unscaledDeltaTimeSeries != null)
                    unscaledDeltaTimeSeries.AddTime(Time.unscaledDeltaTime);
                StopMeasureIfNecessary();
            }
        }

        void FixedUpdate()
        {
            if (isMeasuring)
            {
                var agxStats = agx.Statistics.instance();
                var simTime = agxStats.getTimingInfo("Simulation", "Step forward time");

                if (agxStepForwardSeries != null)
                    agxStepForwardSeries.AddTime((float)(simTime.current * 0.001));

                StopMeasureIfNecessary();
            }
        }

        void StopMeasureIfNecessary()
        {
            timeMeasured = Time.realtimeSinceStartupAsDouble - measureStartTime;
            if (isMeasuring && autoStop && timeMeasured >= timeUntilStop)
            {
                StopMeasure();
            }
        }

        public void StartMeasure()
        {
            agx.Statistics.instance().setEnable(true);

            double secondsCapacity = autoStop ? timeUntilStop : 120.0;
            unscaledDeltaTimeSeries = new TimeSeries((int)(300.0 * secondsCapacity));
            agxStepForwardSeries = new TimeSeries((int)(50.0 * secondsCapacity));
            isMeasuring = true;
            measureStartTime = Time.realtimeSinceStartupAsDouble;
        }

        public void StopMeasure()
        {
            isMeasuring = false;

            if (unscaledDeltaTimeSeries != null)
                unscaledDeltaTimeSeries.ComputeResult();
            if (agxStepForwardSeries != null)
                agxStepForwardSeries.ComputeResult();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PerformanceMeasure))]
    public class PerformanceMeasureEditor : Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            var data = (PerformanceMeasure)target;

            // デフォルトGUIを表示する
            base.OnInspectorGUI();

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button(data.isMeasuring ? "Stop Measure" : "Start Measure"))
            {
                if (data.isMeasuring)
                    data.StopMeasure();
                else
                    data.StartMeasure();
            }

            if(data.isMeasuring || data.unscaledDeltaTimeSeries != null)
            {
                EditorGUILayout.LabelField("Time Measured:", $"{data.timeMeasured : 0.###} s");
            }

            DisplayTimeSeriesStatistics(data.unscaledDeltaTimeSeries, "Delta Time (Main Loop)");
            DisplayTimeSeriesStatistics(data.agxStepForwardSeries, "Agx Step Forward");
        }

        void DisplayTimeSeriesStatistics(PerformanceMeasure.TimeSeries timeSeries, string caption)
        {
            if (timeSeries != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(caption, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Min:", (timeSeries.min * 1000).ToString("0.###") + " ms");
                EditorGUILayout.LabelField("Max:", (timeSeries.max * 1000).ToString("0.###") + " ms");
                EditorGUILayout.LabelField("Average:", (timeSeries.average * 1000).ToString("0.###") + " ms");
                EditorGUILayout.LabelField("Median:", (timeSeries.median * 1000).ToString("0.###") + " ms");
            }
        }
    }
#endif

}