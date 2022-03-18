using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PWRISimulator.ROS
{
    public enum RealTimeDataAccessType { Closest, Previous, Next, Interpolate /*, Extrapolate*/ };

    /// <summary>
    /// 時系列のリアルタイムデータを管理するクラス。例えば、２つの外れるタイムラインの間データを共有できるようにする。
    /// 
    /// データプロジューサーがAdd(data, time)メソッドで新しいデータを時系列バッファに入れ、データコンシューマがGet(time)
    /// メソッドでデータを取得する使い方となる。AddとGetは別々なスレッドから呼び出せる。Getしたデータより古いデータが自動的に
    /// 削除される。
    /// </summary>
    /// <typeparam name="T">データのタイプ</typeparam>
    public class RealTimeDataBuffer<T>
    {
        public delegate T Interpolator(T a, T b, double t);

        struct Entry
        {
            public double time;
            public T data;
            public Entry(double time, T data)
            {
                this.time = time;
                this.data = data;
            }

            public override string ToString()
            {
                return $"time = {time}, data = {data}";
            }
        };

        readonly LinkedList<Entry> buffer;
        readonly object bufferLock = new object();
        readonly int maxBufferSize = 1000;
        readonly Interpolator interpolator = null;
        readonly RealTimeDataAccessType accessType = RealTimeDataAccessType.Previous;

        bool removeHistoryWhenReading = true;
        bool clampToFirst = true;
        bool clampToLast = true;
        
        public RealTimeDataBuffer(int maxBufferSize, RealTimeDataAccessType accessType, 
            Interpolator interpolator = null)
        {
            if (accessType == RealTimeDataAccessType.Interpolate && interpolator == null)
                Debug.LogError($"{GetType().Name} : Cannot use Interpolate access type without an interpolator.");

            buffer = new LinkedList<Entry>();
            this.maxBufferSize = maxBufferSize;
            this.accessType = accessType;
            this.interpolator = interpolator;
        }

        public bool Add(T data, double time)
        {
            Entry entry = new Entry(time, data);
            int errorCode = 0;
            lock (bufferLock)
            {
                if(buffer.Count == 0 || time > buffer.Last.Value.time)
                {
                    buffer.AddLast(entry);
                    if (buffer.Count > maxBufferSize)
                        buffer.RemoveFirst();
                }
                else if(time == buffer.Last.Value.time)
                    buffer.Last.Value = entry;
                else /*if(time < buffer.Last.Value.time)*/
                    errorCode = 1;
            }

            if (errorCode == 1)
            {
                Debug.LogError($"{GetType().Name} : Trying to insert a value with a timestamp older ({time}) than " +
                               $"the most recent value's timestamp. The value will not be added.");
                return false;
            }
            else
            {
                return true;
            }
        }

        public T Get(double time)
        {
            T data = default(T);
            lock (bufferLock)
            {
                // サイズは０の場合
                if (buffer.Count == 0)
                {
                    //Debug.Log("Trying to get value from an empty RealTimeDataBuffer. Will return default value.");
                    return default(T);
                }
                // 指示のtimeは現在の時系列より古い場合
                else if (time < buffer.First.Value.time)
                {
                    if (clampToFirst)
                        return buffer.First.Value.data;
                    else
                    {
                        Debug.LogWarning($"{GetType().Name} : Trying to get a value from  with a timestamp older " + 
                                         "than the oldest value. Default value will be returned.");
                        return default(T);
                    }
                }
                // 指示のtimeは現在の時系列より新しい場合
                else if (time > buffer.Last.Value.time)
                {
                    // 昔のデータを削除
                    if (removeHistoryWhenReading)
                        while (buffer.Last.Previous != null)
                            buffer.Remove(buffer.Last.Previous);

                    if (clampToLast)
                        return buffer.Last.Value.data;
                    else
                    {
                        Debug.LogWarning($"{GetType().Name} : Trying to get a value with a timestamp newer than the " + 
                                          "most recent value. Default value will be returned.");
                        return default(T);
                    }
                }
                else
                {
                    // 上記以外、timeを合わせて要素を検索
                    LinkedListNode<Entry> node = buffer.First;
                    while (node != null)
                    {
                        // 指示のtimeの完璧なマッチの場合
                        if (time == node.Value.time)
                        {
                            data = node.Value.data;
                            break;
                        }
                        // 指示のtimeが前の要素より大きくて次の要素より小さい場合
                        else if (node.Next != null && time < node.Next.Value.time)
                        {
                            // AccessTypeによって隣の要素からデータを取得
                            if (accessType == RealTimeDataAccessType.Previous)
                                data = node.Value.data;
                            else if (accessType == RealTimeDataAccessType.Next)
                                data = node.Next.Value.data;
                            else if (accessType == RealTimeDataAccessType.Closest)
                                data = Math.Abs(time - node.Next.Value.time) < Math.Abs(time - node.Previous.Value.time) ?
                                    node.Next.Value.data :
                                    node.Value.data;
                            else if (accessType == RealTimeDataAccessType.Interpolate)
                            {
                                double t = (time - node.Value.time) / (node.Next.Value.time - node.Value.time);
                                data = interpolator(node.Value.data, node.Next.Value.data, t);
                            }

                            // 昔のデータを削除
                            if (removeHistoryWhenReading)
                                while (node.Previous != null)
                                    buffer.Remove(node.Previous);
                            // データを見つけたのでループをやめる
                            break;
                        }
                        else
                        {
                            // データをまだ見つけていないのでループを続ける
                            node = node.Next;
                            Debug.Assert(node != null, $"{GetType().Name} : Unexpected code reached.");
                        }
                    }
                }
            }
            return data;
        }

        void Clear()
        {
            lock (bufferLock)
            {
                buffer.Clear();
            }
        }
    }
}