using AGXUnity;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Providers.LinearAlgebra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using Debug = UnityEngine.Debug;

namespace PWRISimulator
{
    /// <summary>
    /// クローラダンプの走行スコアリング処理
    /// </summary>
    public class DrivingScore : MonoBehaviour
    {
        // 位置を保持
        private int prevPosX;
        private int prevPosY;

        // 進入不可エリアに滞在した時間
        private float stayTime;

        // 積載量を保持
        private double prevVolume;
        private double volScore;

        // 接触した時間を保持
        private float sepTime;
        private static float sepNotRef = 1.0f;


        // メッシュサイズの都合によるスコア積算

        // 泥濘エリア
        private double mudScore;


        private void scoringDumpSoil()
        {
            // 積載量取得
            var obj = this.transform.parent.parent.gameObject;
            var ds = obj.GetComponentInChildren<DumpSoil>();
            double volume = ds.soilVolume;

            if (volume >= prevVolume)
            {
                // 積載量の増加分
                var diff = volume - prevVolume;

                if (diff > 0.0)
                {
                    // 増加差分は一度だけスコアへ積算する。
                    volScore += GlobalVariables.LoadSoilCoef * diff;

                    Debug.Log("***** scoringDumpSoil: " + volScore + " *****");
                    Debug.Log("volume: " + volume + ", prevVolume: " + prevVolume + ", diff: " + diff);
                }

                // 1点未満の増加も評価済みにし、次フレームで重複加算しない。
                prevVolume = volume;

                if (Math.Abs(volScore) >= 1.0)
                {
                    // スコア反映
                    GlobalVariables.RegisterScoreEvent(new GlobalVariables.ScoreEvent { Id = GlobalVariables.ScoreEventId.P02, Point = (int)volScore });

                    // スコア積算リセット
                    volScore = volScore - (int)volScore;
                    
                }
            }
            else {
                // 積載量が減少した場合は値の保持のみ
                prevVolume = volume;
            }
        }

        private void scoringMuddyAreas(double x, double y)
        {
            // メッシュを0.5mで計算
            int _x = (int)(x / 0.5);
            int _y = (int)(y / 0.5);

            // 泥濘エリアで停止している場合は二重カウントしないようにする
            if (_x != prevPosX || _y != prevPosY)
            {
                // 移動したらカウントアップ
                GlobalVariables.setCountMat(_x, _y);

                Debug.Log("***** Mad Count: " + GlobalVariables.countMat[_x, _y] + ", index: " + _x + ", " + _y + " *****");

                // スコア計算
                if (GlobalVariables.getCountMat(_x, _y) > 1.0)
                {
                    // カウントが2以上になったら重畳
                    mudScore += GlobalVariables.OverlappCoef * 0.5;

                    Debug.Log("***** mudScore: " + mudScore + " *****");

                    if (Math.Abs(mudScore) > 0.5)
                    {
                        // スコア反映
                        GlobalVariables.RegisterScoreEvent(new GlobalVariables.ScoreEvent { Id = GlobalVariables.ScoreEventId.M04, Point = (int)mudScore });

                        // スコア積算リセット
                        mudScore = mudScore - (int)mudScore;
                    }
                }

                prevPosX = _x;
                prevPosY = _y;
            }
        }

        private void scoringRestrictedAreas()
        {
            // 経過時間の加算
            stayTime += Time.deltaTime;

            Debug.Log("stayTime: " + stayTime);

            // スコア計算
            if ((int)stayTime >= 1)
            {
                // 1秒以上経過で減算
                GlobalVariables.RegisterScoreEvent(new GlobalVariables.ScoreEvent { Id = GlobalVariables.ScoreEventId.M03, Point = (int)(GlobalVariables.OffTruckCoef * (int)stayTime) });
                // スコア計算した分は経過時間から引いておく
                stayTime = stayTime - (int)stayTime;
            }
        }

        private void OnSeparation(SeparationData data)
        {
            //UnityEngine.Debug.Log("OnSeparation: " + data);
            //Debug.Log("Component1: " + data.Component1.transform.root.gameObject.name);
            //Debug.Log("Component2: " + data.Component2.transform.root.gameObject.name);

            var com_1 = data.Component1.transform.root.gameObject.name;
            var com_2 = data.Component2.transform.root.gameObject.name;

            Debug.Log("com_1: " + com_1 + ", com_2: " + com_2);
            //Debug.Log("Time.time: " + Time.time + ", sepTime: " + sepTime + ", sepNotRef: " + sepNotRef);

            float diff = Time.time - sepTime;

            if (diff >= sepNotRef)
            {
                Debug.Log("com_1: " + com_1 + ", com_2: " + com_2);

                // 重機名確認
                if ((com_1.Contains("ic120") || com_1.Contains("zx200")) &&
                    (com_2.Contains("ic120") || com_2.Contains("zx200")) &&
                    com_1 != com_2)
                {
                    // 他の重機との接触

                    // スコア計算
                    GlobalVariables.RegisterScoreEvent(new GlobalVariables.ScoreEvent { Id = GlobalVariables.ScoreEventId.M02, Point = (int)GlobalVariables.CollisionCoef });

                    sepTime = Time.time;
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // 初期化
            volScore = 0.0;
            stayTime = 0.0f;
            mudScore = 0.0;
            sepTime = 0.0f;

            // 親オブジェクト取得
            var parent = this.transform.parent.gameObject;


            // 接触判定に使用するbody_linkを取得
            //var body_link = parent.transform.Find("body_link").gameObject;

            //// 他の重機との接触判定
            //var rb = body_link.GetComponent<RigidBody>();
            //if (rb == null)
            //{
            //    Debug.LogWarning("MyContactListener: Expecting a RigidBody component.", this);
            //    return;
            //}
            //Debug.Log("Modifying surface velocity of " + rb.name + ".");

            //// コールバックを設定
            //Simulation.Instance.ContactCallbacks.OnSeparation(OnSeparation, rb);


            // RigidBodyを取得
            var RGBL = parent.GetComponentsInChildren<RigidBody>();
            foreach (RigidBody rgb in RGBL)
            {
                Debug.Log(rgb);

                // コールバックを設定
                Simulation.Instance.ContactCallbacks.OnSeparation(OnSeparation, rgb);
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (GlobalVariables.ActionMode == 3)
            {
                Debug.Log("prevPosX: " + prevPosX + ", prevPosY: " + prevPosY + ", Object: " + this.gameObject);

                // 現在地を取得
                double Xpos = this.gameObject.transform.position.x;
                double Ypos = this.gameObject.transform.position.z;

                // エリア確認
                int x_idx = (int)(Xpos / GlobalVariables.step_x);
                int z_idx = (int)(Ypos / GlobalVariables.step_z);

                int curtArea = (int)GlobalVariables.getAreaMat(x_idx, z_idx);

                //Debug.Log("curtArea: " + curtArea + ", Position: (" + Xpos + ", " + Ypos + ")");
                Debug.Log("curtArea: " + curtArea + ", " + this.gameObject.transform.parent.parent.gameObject);

                //--------------------
                // エリアごとの処理
                //--------------------
                if (curtArea == 2)
                {
                    // 進入不可エリア
                    scoringRestrictedAreas();
                }
                else if (curtArea == 5)
                {
                    // 泥濘エリア
                    scoringMuddyAreas(Xpos, Ypos);
                }

                // 進入不可エリアでない場合は経過時間をリセット
                if (curtArea != 2) {
                    stayTime = 0.0f;
                }

                //--------------------
                // 積載量のスコアリング
                //--------------------
                scoringDumpSoil();
            }
        }
    }
}
