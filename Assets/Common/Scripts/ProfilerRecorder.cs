// Copyright 2021 VMC Motion Technologies Co., Ltd.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Profiling;
using System.Text;

namespace VMT.Profiling
{
    /// <summary>
    /// カスタムのProfilerMarker、Unityの標準Profilerサンプラーの測定値を取得してコンソールにプリントするクラス。
    /// </summary>
    /// <example>FixedBehaviourUpdateのstringをsamplerNamesに挿入してFixedUpdate()メソッドにかかる時間を確認できる</example>
    public class ProfilerRecorder : MonoBehaviour
    {
        /// <summary>
        /// 実測するプロファイルmarker/sampler名。Play時に変更することができない。
        /// </summary>
        public List<string> samplerNames;
        public bool printToConsole = true;
        public bool printOnlyIfNonZeroData = false;
        
        Dictionary<string, Recorder> recorders;

        void Start()
        {
            recorders = new Dictionary<string, Recorder>(samplerNames.Count);
            foreach (string sampler in samplerNames)
            {
                recorders[sampler] = Recorder.Get(sampler);
            }
        }

        void Update()
        {
            if (printToConsole)
                PrintToConsole();
        }

        public void PrintToConsole()
        {
            if (printOnlyIfNonZeroData)
            {
                bool hasNonZeroData = false;
                foreach (var recorder in recorders)
                {
                    if (recorder.Value.elapsedNanoseconds != 0)
                    {
                        hasNonZeroData = true;
                        break;
                    }
                }
                if (!hasNonZeroData)
                    return;
            }

            var str = new StringBuilder(recorders.Count * 50);
            foreach (var recorder in recorders)
            {
                if (recorder.Value.elapsedNanoseconds > 0)
                    str.Append($"Recorder: {recorder.Key} = {recorder.Value.elapsedNanoseconds * (1e-6f): 0.00} ms ");
                else
                    str.Append($"Recorder: {recorder.Key} =      ms ");
            }
            Debug.Log(str.ToString());
        }
    }
}
