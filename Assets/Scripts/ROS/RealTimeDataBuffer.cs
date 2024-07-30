using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

namespace PWRISimulator.ROS
{
    public enum RealTimeDataAccessType { Closest, Previous, Next, Interpolate /*, Extrapolate*/ };

    /// <summary>
    /// データを登録する時に時刻も同時に記録するリスト。時刻は実時間を使用する
    /// データを取り出す際、引数として渡した時刻とリスト項目の時刻を比較し、適切な項目のデータを返す
    /// データを取り出す際、引数として渡した時刻より前の時刻のリスト項目は自動的に削除する
    /// (挙動としてはQueueに近いが、単純に直近のデータを返すだけでなく、補間などする場合もある点に注意)
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
            public double AddedTime;
            public T data;
            public Entry(double time, T data)
            {
                this.AddedTime = time;
                this.data = data;
            }

            public override string ToString()
            {
                return $"time = {AddedTime}, data = {data}";
            }
        };

        readonly LinkedList<Entry> buffer;
        readonly object bufferLock = new();
        readonly int maxBufferSize = 1000;
        readonly Interpolator interpolator = null;
        readonly RealTimeDataAccessType accessType = RealTimeDataAccessType.Previous;
        
        public RealTimeDataBuffer(int aMaxBufferSize, RealTimeDataAccessType aAccessType, Interpolator aInterpolator = null)
        {
            if (accessType == RealTimeDataAccessType.Interpolate && interpolator == null)
                Debug.LogError($"{GetType().Name} : Cannot use Interpolate access type without an interpolator.");

            buffer = new LinkedList<Entry>();
            maxBufferSize = aMaxBufferSize;
            accessType = aAccessType;
            interpolator = aInterpolator;
        }

        public bool Add(T newData, double currentRealTime)
        {
            Entry entry = new Entry(currentRealTime, newData);
            lock (bufferLock)
            {
                if(buffer.Count == 0 || currentRealTime > buffer.Last.Value.AddedTime)
                {
                    // add new
                    buffer.AddLast(entry);
                    if (buffer.Count > maxBufferSize)
                        buffer.RemoveFirst();
                    return true;
                }
                else if(currentRealTime == buffer.Last.Value.AddedTime)
                {
                    // replace
                    buffer.Last.Value = entry;
                    return true;
                }
                else /*if(time < buffer.Last.Value.time)*/
                {
                    // discard
                    Debug.LogError($"{GetType().Name} : Trying to insert a value with a timestamp older ({currentRealTime}) than " +
                                    $"the most recent value's timestamp. The value will not be added.");
                    return false;
                }
            }
        }

        public T Get(double inputTime)
        {
            T returnData = default;
            lock (bufferLock)
            {
                // バッファにデータが登録されていない場合
                if (buffer.Count == 0)
                {
                    //Debug.Log("Trying to get value from an empty RealTimeDataBuffer. Will return default value.");
                    return default;
                }
                // 引数の時刻がバッファに保存されている最初のデータより過去の場合
                else if (inputTime < buffer.First.Value.AddedTime)
                {
                    return buffer.First.Value.data;
                }
                // 引数の時刻がバッファに保存されている最後のデータより未来の場合
                else if (inputTime > buffer.Last.Value.AddedTime)
                {
                    // 引数の時刻より過去のデータを全て削除(->最新のデータ以外は全て削除する)
                    while (buffer.Last.Previous != null)
                        buffer.Remove(buffer.Last.Previous);
                    return buffer.Last.Value.data;
                }
                else
                {
                    // 上記以外、引数の時刻を見て検索
                    LinkedListNode<Entry> node = buffer.First;
                    while (node != null)
                    {
                        // 引数の時刻と一致する時刻のデータを発見した
                        if (inputTime == node.Value.AddedTime)
                        {
                            returnData = node.Value.data;
                            break;
                        }
                        // リスト上の次の項目が引数の時刻より未来の場合(かつ、現在の項目は引数の時刻より過去である)
                        else if (node.Next != null && inputTime < node.Next.Value.AddedTime)
                        {
                            // AccessTypeによって取得するデータを決める
                            switch (accessType) 
                            {
                                case RealTimeDataAccessType.Previous: // 直前
                                    returnData = node.Value.data;
                                    break;
                                case RealTimeDataAccessType.Next: // 直後
                                    returnData = node.Next.Value.data;
                                    break;
                                case RealTimeDataAccessType.Closest: // 最近
                                    double diffNext = Math.Abs(inputTime - node.Next.Value.AddedTime);
                                    double diffCurrent = Math.Abs(inputTime - node.Value.AddedTime);
                                    returnData = diffNext < diffCurrent ? node.Next.Value.data : node.Value.data;
                                    break;
                                case RealTimeDataAccessType.Interpolate: // 線形補間
                                    double t = (inputTime - node.Value.AddedTime) / (node.Next.Value.AddedTime - node.Value.AddedTime);
                                    returnData = interpolator(node.Value.data, node.Next.Value.data, t);
                                    break;
                            }

                            // 現在のデータより過去のデータを削除
                            while (node.Previous != null)
                                buffer.Remove(node.Previous);
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
            return returnData;
        }
    }
}