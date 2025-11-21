using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 時系列データを「受信→internalDeadTime経過後に取り出し」する遅延バッファ
/// 使い方:
///   var joint_data = new DeadTimeDelay<MyType>(internalDeadTimeMs);
///   joint_data.deadTimeDelay (deadTimeMs)           // むだ時間を設定 (deadTimeMs: ミリ秒)
///   joint_data.addInputData (nowMs, value);         // 受信時に追加（nowMs: ミリ秒）
///   var ready = joint_data.drainInputData (nowMs);      // 取り出し時に現在時刻を渡すと、満了分が順に出力
/// </summary>
public class DeadTimeDelay<T>
{
    private readonly Queue<(double timestampMs, T data)> _queue = new();
    private readonly double _internalDeadTimeMs;

    /// <param name="DeadTimeMs">
    /// このバッファ内で待つむだ時間（ミリ秒）。0以下なら即時通過（DrainReady呼出時にすべて取り出し）。
    /// </param>
    public DeadTimeDelay(double deadTimeMs)
    {
        _internalDeadTimeMs = deadTimeMs;
    }

    /// <summary>
    /// 呼び出し側から現在時刻[ms]とデータを追加
    /// </summary>
    public void addInputData(double timestampMs, T data)
    {
        _queue.Enqueue((timestampMs, data));
    }

    /// <summary>
    /// 現在時刻[nowMs]時点で「(now - ts) >= DeadTimeMs」を満たすものを
    /// FIFO順で全て取り出して返す。DeadTimeMs <= 0 の場合は全件即取り出し。
    /// </summary>
    public bool drainInputData(double nowMs, out List<T> ready)
    {
        ready = new List<T>();
        while (_queue.Count > 0)
        {
            var (ts, data) = _queue.Peek();
            if ((nowMs - ts) >= _internalDeadTimeMs)
            {
                ready.Add(data);
                _queue.Dequeue();
                return true;
            }
            else if (_queue.Count <= 0 || (nowMs - ts) < _internalDeadTimeMs) 
            {
                break; 
            }
            else
            {
                break; 
            }
        }
        return false;
    }

    /// <summary>期限到来分の「最後の1件」だけ取得。なければ fallback。</summary>
    public bool drainInputDataLatest(double nowMs, out T data, T fallback = default)
    {
        if (drainInputData(nowMs, out List<T> list))
        {
            data = list.Count > 0 ? list[list.Count - 1] : fallback;
            return true;
        }
        data = fallback;
        return false;
    }

    /// <summary>
    /// 内部バッファを破棄
    /// </summary>
    public void Clear() => _queue.Clear();
}
