using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


namespace PWRISimulator
{
    /// <summary>
    /// グローバル変数管理
    /// </summary>
    public class GlobalVariables
    {
        // add 202507
        // 配置したダンプトラックオブジェクトの保持
        public static List<string> Dump_IDList = new List<string>();
        public static List<GameObject> Dump_ObjList = new List<GameObject>();

        // セーブ・ロード機能での姿勢読み込みフラグ
        public static bool SetupJointFlag = false;
        public static bool SetupJointDumpFlag = false;
        public static int SetupJointDumpCount = 0;

        // セーブ・ロード機能での姿勢読み込み完了フラグ
        public static bool SetupJointCompletedFlag = false;
        public static bool SetupJointDumpCompletedFlag = false;

        public static saveScript.SaveMachines saveMachines = new saveScript.SaveMachines();
        public static saveScript.SaveDumpSoil saveDumpSoil = new saveScript.SaveDumpSoil();
        public static saveScript.SaveParticles saveParticles = new saveScript.SaveParticles();

        public static Dictionary<string, saveScript.MachineCameraSliderState> MachineCameraSliders
            = new Dictionary<string, saveScript.MachineCameraSliderState>();

        //public static bool ObjectRemoveFlag = false;
        public static int ConfirmWaitFlag = 0; // 0: 初期値、1：確認画面表示中、2：確認画面OKクリック


        // ファイル出力時のフォルダパス
        public static string BACKUP_FOLDER = "Assets/SaveData/";


        // エリア判定行列
        public static MathNet.Numerics.LinearAlgebra.Matrix<double> areaMat;

        // ピクセル間の距離
        public static double step_x = 0.0;
        public static double step_z = 0.0;

        // 泥濘エリアのカウント行列
        public static MathNet.Numerics.LinearAlgebra.Matrix<double> countMat;

        private static Mutex _mutexAreaMat = new Mutex();
        private static Mutex _mutexCountMat = new Mutex();


        public static double getAreaMat(int x, int y)
        {
            double val = -1.0;

            if (_mutexAreaMat.WaitOne(TimeOutSpan))
            {
                try
                {
                    val = areaMat[x, y];
                }
                finally
                {
                    _mutexAreaMat.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexAreaMat.");
            }

            return val;
        }


        public static void setCountMat(int x, int y)
        {
            if (_mutexCountMat.WaitOne(TimeOutSpan))
            {
                try
                {
                    countMat[x, y] += 1.0;
                }
                finally
                {
                    _mutexCountMat.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexCountMat.");
            }
        }

        public static double getCountMat(int x, int y)
        {
            double val = -1.0;

            if (_mutexCountMat.WaitOne(TimeOutSpan))
            {
                try
                {
                    val = countMat[x, y];
                }
                finally
                {
                    _mutexCountMat.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexCountMat.");
            }

            return val;
        }




        //設定データ
        public static int MaxDunpTracks;   //最大設置可能ダンプトラック数
        public static int MaxCameras;      //最大設置可能カメラ数
        public static int MinScore;      //最大設置可能カメラ数
        public static float MiningCoef;    //採掘スコア係数
        public static float LoadSoilCoef;  //土砂積込みスコア係数
        public static float UnloadSoilCoef;//土砂積み降ろしスコア係数
        public static float CollisionCoef; //重機衝突スコア係数
        public static float OffTruckCoef;  //コースアウトスコア係数
        public static float OverlappCoef;  //コース重複スコア係数
        public static string datapath;     //データ保存パス
        public static string RosIP;        //ROS接続先IP
        public static float GameTime;               //ゲーム時間（秒）      
        public static float TimeBarRedThreshold;      //タイムバーの赤色への切り替え割合（％）
        public static float TimeBarYellowThreshold;   //タイムバーの黄色への切り替え割合（％）


        public static int score = 0;

        public static double OutOfFieldAreaTime = 0.0;

        public static double AmountOfPickupSoil = 0.0;

        public static double AmountOfDropedSoil = 0.0;

        public static double AmountOfTransportedSoil = 0.0;

        public static double AmountOfLoadedSoil = 0.0;

        public static int ActionMode = -1; //0:トラック配置,　1:カメラ配置,　2:使用カメラ選択,　3:シミュレーション
        public static int SelectMode = -1; //0:セーブ,　1:ロード,　2:リセット
        public static int SetMoveType = 0; //0:並進,　1:回転,　2:削除,　3:メインカメラ

        public static int TimeOutSpan = 100;

        public static bool CameraSelected = false;

        public static int ic120Counter = 0;
        public static int CameraCounter = 0;

        public static bool ForceCameraChange = false;

        // ScoreBoard（総獲得点）の表示フラグ。シミュレーション開始前にメニュー UI のチェックボックスで切り替える。
        public static bool ShowScoreBoard = true;

        private static Mutex _mutexScore = new Mutex();
        private static Mutex _mutexOOFAT = new Mutex();
        private static Mutex _mutexAOPS = new Mutex();
        private static Mutex _mutexAODS = new Mutex();
        private static Mutex _mutexAOTS = new Mutex();
        private static Mutex _mutexAOLS = new Mutex();
        private static Mutex _mutexActionMode = new Mutex();

        public static void incrementScore(int point)
        {
            if (_mutexScore.WaitOne(TimeOutSpan))
            {
                try
                {
                    // スコア下限値の場合は減算しない
                    if (score <= MinScore && point < 0) {
                        score = MinScore;
                        return;
                    }

                    score = score + point;
                }
                finally
                {
                    _mutexScore.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexScore.");
            }
        }

        public enum ScoreEventId { P01, P02, P03, M01, M02, M03, M04 }

        public struct ScoreEvent
        {
            public ScoreEventId Id;
            public int Point;
        }

        public static event System.Action<ScoreEvent> OnScoreEvent;

        public static void RegisterScoreEvent(ScoreEvent evt)
        {
            incrementScore(evt.Point);
            OnScoreEvent?.Invoke(evt);
        }

        public static void decrementScore(int point)
        {
            if (_mutexScore.WaitOne(TimeOutSpan))
            {
                try
                {
                    score = score - point;
                }
                finally
                {
                    _mutexScore.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexScore.");
            }
        }


        public static void incrementOutOfFieldAreaTime(double point)
        {
            if (_mutexOOFAT.WaitOne(TimeOutSpan))
            {
                try
                {
                    OutOfFieldAreaTime = OutOfFieldAreaTime + point;
                }
                finally
                {
                    _mutexOOFAT.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexOOFAT.");
            }
        }


        public static void incrementAmountOfPickupSoil(double point)
        {
            if (_mutexAOPS.WaitOne(TimeOutSpan))
            {
                try
                {
                    AmountOfPickupSoil = AmountOfPickupSoil + point;
                }
                finally
                {
                    _mutexAOPS.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexAOPS.");
            }
        }

        public static void decrementAmountOfPickupSoil(double point)
        {
            if (_mutexAOPS.WaitOne(TimeOutSpan))
            {
                try
                {
                    AmountOfPickupSoil = AmountOfPickupSoil - point;
                }
                finally
                {
                    _mutexAOPS.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexAOPS.");
            }
        }

        public static void changeActionMode(int mode)
        {
            if (_mutexActionMode.WaitOne(TimeOutSpan))
            {
                try
                {
                    ActionMode = mode;
                }
                finally
                {
                    _mutexActionMode.ReleaseMutex();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Error : Could not get _mutexActionMode.");
            }

        }



    }
}
