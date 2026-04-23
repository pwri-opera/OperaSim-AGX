using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace PWRISimulator
{
    /// <summary>
    /// 油圧ショベルのスコアリング処理
    /// </summary>
    public class ShovelScore : MonoBehaviour
    {
        // 位置を保持
        private int prevPosX;
        private int prevPosY;

        // 進入不可エリアに滞在した時間
        private float stayTime;


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



        // Start is called before the first frame update
        void Start()
        {
            // 初期化
            stayTime = 0.0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (GlobalVariables.ActionMode == 3)
            {
                //Debug.Log("prevPosX: " + prevPosX + ", prevPosY: " + prevPosY + ", Object: " + this.gameObject);

                // 現在地を取得
                double Xpos = this.gameObject.transform.position.x;
                double Ypos = this.gameObject.transform.position.z;

                // エリア確認
                int x_idx = (int)(Xpos / GlobalVariables.step_x);
                int z_idx = (int)(Ypos / GlobalVariables.step_z);

                int curtArea = (int)GlobalVariables.getAreaMat(x_idx, z_idx);

                //Debug.Log("curtArea: " + curtArea + ", Position: (" + Xpos + ", " + Ypos + ")");


                //--------------------
                // エリアごとの処理
                //--------------------
                if (curtArea == 2)
                {
                    // 進入不可エリア
                    scoringRestrictedAreas();
                }

                // 進入不可エリアでない場合は経過時間をリセット
                if (curtArea != 2)
                {
                    stayTime = 0.0f;
                }
            }
        }
    }
}
