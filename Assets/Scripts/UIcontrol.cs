using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using PWRISimulator.ROS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using static PWRISimulator.UIcontrol;
using static System.Net.Mime.MediaTypeNames;
//using UnityEngine.EventSystems;

using Debug = UnityEngine.Debug;


namespace PWRISimulator
{
    /// <summary>
    /// GUIパーツ制御処理
    /// </summary>
    public class UIcontrol : MonoBehaviour
    {
        [SerializeField] GameObject MenuUI;
        private UIDocument _uiDocument;
        private GameObject munuUIobj;

        [SerializeField] GameObject CameraSelectorUI;
        private UIDocument _uiCameraSelectDocument;
        private GameObject CameraSelectorUIobj;

        [SerializeField] GameObject MessageDaialogUI;
        private GameObject MessageDaialogUIobj;
        private UIDocument _uiMessageDaialogDocument;

        [SerializeField] GameObject CountdownTimerUI;
        private GameObject CountdownTimerUIobj;
        private UIDocument _uiCountdownTimerDocument;


        //[SerializeField] GameObject CountdownTimerUI_disp2;
       // private GameObject CountdownTimerUIobj_disp2;
        //private UIDocument _uiCountdownTimerDocument_disp2;

        [SerializeField] GameObject ScoreUI;
        private GameObject ScoreUIobj;

        //[SerializeField] GameObject ScoreUI_disp2;
        //private GameObject ScoreUIobj_disp2;

        [SerializeField] GameObject StatusBoardUI;
        private GameObject StatusBoardUIobj;
        private StatusBoard sBoard;

        [SerializeField] GameObject EventLogUI;
        private GameObject EventLogUIobj;

        [SerializeField] GameObject SubdisplayUI;
        private GameObject SubdisplayUIobj;


        // セーブ・ロード機能 シミュレーションUI
        [SerializeField] GameObject SaveLoadUI;
        private GameObject SaveLoadUIobj;
        private UIDocument _uiSaveLoadDocument;

        // セーブ・ロード機能 設定画面UI
        [SerializeField] GameObject SaveLoadSettingUI;
        private GameObject SaveLoadSettingUIobj;
        private UIDocument _uiSaveLoadSettingDocument;


        [SerializeField] GameObject ConfirmDaialogUI;
        private GameObject ConfirmDaialogUIobj;
        private UIDocument _uiConfirmDaialogDocument;


        // 並進・回転・削除
        [SerializeField] GameObject ObjectTransRotUI;
        private GameObject ObjectTransRotUIobj;
        private UIDocument _uiObjectTransRotDocument;


        private int prev_SetMoveType;
        private DeformableTerrain terrain;

        private MachineObjCamCont machineObj;



        // Start is called before the first frame update
        void Start()
        {
            // シーン再ロード時に前回の倍率が残らないようにリセット
            Time.timeScale = 1.0f;
            GlobalVariables.SimulationSpeedMultiplier = 1.0f;

            machineObj = new MachineObjCamCont();

            StatusBoardUIobj = Instantiate(StatusBoardUI);
            sBoard = StatusBoardUIobj.GetComponent<StatusBoard>();
            sBoard.SetStatusMessage("Start!!");
            sBoard.SetMessageColor(Color.blue);

            if (EventLogUI != null)
            {
                EventLogUIobj = Instantiate(EventLogUI);
            }


            munuUIobj = Instantiate(MenuUI);
            _uiDocument = munuUIobj.GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;

            setupMenuUI();
            //root.Q<Button>("DunpTruckPositionSetting").clicked += () => OnDunpTruckPositionSettingClicked();
            //root.Q<Button>("CameraPositionSetting").clicked += () => OnCameraPositionSettingClicked();
            //root.Q<Button>("SelectCamera").clicked += () => OnSelectCameraClicked();
            //root.Q<Button>("SimulationStart").clicked += () => OnSimulationStartClicked();
            //root.Q<Button>("send").clicked += () => Debug.Log(root.Q<TextField>("TitleText").value);

            //GlobalVariables.ActionMode = -1;
            GlobalVariables.changeActionMode(-1);

            SubdisplayUIobj = null;

            prev_SetMoveType = GlobalVariables.SetMoveType;


            // セーブ・ロード・リセット機能
            SaveLoadSettingUIobj = Instantiate(SaveLoadSettingUI);

            _uiSaveLoadSettingDocument = SaveLoadSettingUIobj.GetComponent<UIDocument>();
            _uiSaveLoadSettingDocument.gameObject.SetActive(true);

            var save_root = _uiSaveLoadSettingDocument.rootVisualElement;

            save_root.Q<Button>("Save").clicked += () => SaveClicked();
            save_root.Q<Button>("Load").clicked += () => LoadClicked();
            save_root.Q<Button>("Reset").clicked += () => ResetClicked();
        }

        // Update is called once per frame
        void Update()
        {
            if (GlobalVariables.ActionMode != 3 && GlobalVariables.SetMoveType != prev_SetMoveType)
            {
                Debug.Log("SetMoveType: " + GlobalVariables.SetMoveType + ", prev: " + prev_SetMoveType);

                if (ObjectTransRotUIobj != null)
                {
                    if (_uiObjectTransRotDocument.gameObject.activeSelf)
                    {
                        if (GlobalVariables.SetMoveType == 0)
                        {
                            ObjTranslationClicked();
                        }
                        else if (GlobalVariables.SetMoveType == 1)
                        {
                            ObjRotationClicked();
                        }
                        else if (GlobalVariables.SetMoveType == 2)
                        {
                            ObjDeleteClicked();
                        }
                        else if (GlobalVariables.SetMoveType == 3)
                        {
                            MainCameraSettingsClicked();
                        }
                    }
                }
            }

            prev_SetMoveType = GlobalVariables.SetMoveType;

            if (CountdownTimer.timeupFlag) {
                // 時間切れの場合ボタンを削除
                if (SaveLoadUIobj != null)
                {
                    Destroy(SaveLoadUIobj);
                    SaveLoadUIobj = null;
                }

                // 速度倍率を戻す
                Time.timeScale = 1.0f;
            }

            EndGame();
        }

        // Play を抜けるときに倍率が Unity のプロジェクト設定へ書き戻されるのを防ぐ
        void OnApplicationQuit()
        {
            Time.timeScale = 1.0f;
        }


        void OnDunpTruckPositionSettingClicked()
        {
            if (SubdisplayUIobj != null)
            {
                Destroy(SubdisplayUIobj);
                SubdisplayUIobj = null;
            }

            machineObj.machineDeselected();

            //GlobalVariables.ActionMode = 0;
            GlobalVariables.changeActionMode(0);
            Debug.Log("Score: " + GlobalVariables.ActionMode);

            //sBoard.SetStatusMessage("ダンプトラック配置");
            sBoard.SetStatusMessage("Dump truck Settings");

            SubdisplayUIobj = Instantiate(SubdisplayUI);
            SubdisplayUIobj.name = "SubdisplayForSpawnCamera";
            Debug.Log(SubdisplayUIobj.name);


            // 並進・回転・削除ボタン
            if (ObjectTransRotUIobj == null)
            {
                ObjectTransRotUIobj = Instantiate(ObjectTransRotUI);
                _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();

                SetButtonAddFunction(_uiObjectTransRotDocument);
            }
            else
            {
                _uiObjectTransRotDocument.gameObject.SetActive(true);
                _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();

                SetButtonAddFunction(_uiObjectTransRotDocument);
            }
        }


        void OnCameraPositionSettingClicked()
        {

            if (SubdisplayUIobj != null)
            {
                Destroy(SubdisplayUIobj);
                SubdisplayUIobj = null;
            }

            machineObj.machineDeselected();

            //GlobalVariables.ActionMode = 1;
            GlobalVariables.changeActionMode(1);
            Debug.Log("Score: " + GlobalVariables.ActionMode);

            //sBoard.SetStatusMessage("カメラ配置");
            sBoard.SetStatusMessage("Camera Settings");

            SubdisplayUIobj = Instantiate(SubdisplayUI);
            SubdisplayUIobj.name = "SubdisplayForSpawnCamera";
            Debug.Log(SubdisplayUIobj.name);


            //// 並進・回転・削除ボタン表示
            //_uiObjectTransRotDocument.gameObject.SetActive(true);


            // 並進・回転・削除ボタン
            if (ObjectTransRotUIobj == null)
            {
                ObjectTransRotUIobj = Instantiate(ObjectTransRotUI);
                _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();

                SetButtonAddFunction(_uiObjectTransRotDocument);
            }
            else
            {
                _uiObjectTransRotDocument.gameObject.SetActive(true);
                _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();

                SetButtonAddFunction(_uiObjectTransRotDocument);
            }
        }


        void OnSelectCameraClicked()
        {

            if (SubdisplayUIobj != null)
            {
                Destroy(SubdisplayUIobj);
                SubdisplayUIobj = null;
            }

            machineObj.machineDeselected();

            //GlobalVariables.ActionMode = 2;
            GlobalVariables.changeActionMode(2);
            Debug.Log("Score: " + GlobalVariables.ActionMode);

            _uiDocument.gameObject.SetActive(false);

            // 並進・回転・削除ボタン非表示
            if (_uiObjectTransRotDocument != null)
            {
                _uiObjectTransRotDocument.gameObject.SetActive(false);
            }


            CameraSelectorUIobj = Instantiate(CameraSelectorUI);
            _uiCameraSelectDocument = CameraSelectorUIobj.GetComponent<UIDocument>();
            var camera_root = _uiCameraSelectDocument.rootVisualElement;

            ListView listView = camera_root.Q<ListView>("CameraList");
            List<Camera> cameras = new List<Camera>();
            //cameras.AddRange(FindObjectsOfType<Camera>(true));
            cameras.AddRange(CameraChanger.FindGameCameras(true));
            List<string> CameraNameList = new List<string>();

            camera_root.Q<Button>("CameraSetButton").clicked += () => OnCameraSetClicked(cameras);

            GenerateCheckboxes(_uiCameraSelectDocument, cameras.Count, cameras);

            //sBoard.SetStatusMessage("使用カメラ選択");
            sBoard.SetStatusMessage("Camera Selection");
        }


        void OnSimulationStartClicked()
        {
            if (SaveLoadSettingUIobj != null) {
                Destroy(SaveLoadSettingUIobj);
                SaveLoadSettingUIobj = null;
            }


            if (SubdisplayUIobj != null)
            {
                Destroy(SubdisplayUIobj);
                SubdisplayUIobj = null;
            }

            machineObj.machineDeselected();

            //GlobalVariables.ActionMode = 3;
            GlobalVariables.changeActionMode(3);
            Debug.Log("Score: " + GlobalVariables.ActionMode);

            UnityEngine.Object.Destroy(munuUIobj);

            List<Camera> cameras = new List<Camera>();
            //cameras.AddRange(FindObjectsOfType<Camera>(true));
            cameras.AddRange(CameraChanger.FindGameStreamingCameras(true));

            CountdownTimerUIobj = Instantiate(CountdownTimerUI);
            //CountdownTimerUIobj_disp2 = Instantiate(CountdownTimerUI_disp2);

            CountdownTimerUIobj.SetActive(true);

            //CountdownTimerUIobj.GetComponent<CountdownTimer>().ResetTimer();
            CountdownTimerUIobj.GetComponent<CountdownTimer>().StartTimer();
            //CountdownTimerUIobj_disp2.GetComponent<CountdownTimer>().StartTimer();

            int width = Screen.width;
            int height = Screen.height;

            RenderTexture cameraTexture = new RenderTexture(width, height, 24);

            //CountdownTimerUIobj_disp2.GetComponent<UIDocument>().panelSettings.targetTexture = cameraTexture;

            ScoreUIobj = Instantiate(ScoreUI);
            ScoreUIobj.SetActive(GlobalVariables.ShowScoreBoard);

            //ScoreUIobj_disp2 = Instantiate(ScoreUI_disp2);
            //ScoreUIobj_disp2.GetComponent<UIDocument>().panelSettings.targetTexture = cameraTexture;


            sBoard.DeleteStatusMessage();

            CamerasInvisible();


            // 並進・回転・削除ボタン非表示
            if (_uiObjectTransRotDocument != null)
            {
                _uiObjectTransRotDocument.gameObject.SetActive(false);
            }

            GlobalVariables.SetMoveType = 0;


            // セーブ・ロード・リセット機能
            SaveLoadUIobj = Instantiate(SaveLoadUI);

            _uiSaveLoadDocument = SaveLoadUIobj.GetComponent<UIDocument>();
            _uiSaveLoadDocument.gameObject.SetActive(true);

            var save_root = _uiSaveLoadDocument.rootVisualElement;

            save_root.Q<Button>("Save").clicked += () => SaveClicked();
            save_root.Q<Button>("Load").clicked += () => LoadClicked();
            save_root.Q<Button>("Reset").clicked += () => ResetClicked();

            // シミュレーション速度倍率を適用し、リアルタイム性確認用プローブを起動
            Time.timeScale = GlobalVariables.SimulationSpeedMultiplier;
            if (GlobalVariables.SimulationSpeedMultiplier != 1.0f)
                ValidateSimulationSteppingSettings();
            if (GetComponent<RealtimeFidelityProbe>() == null)
                gameObject.AddComponent<RealtimeFidelityProbe>();
        }

        /// <summary>
        /// 速度倍率はFixedUpdate毎にAGXが1ステップ進むことに依存する(ref #55)。
        /// FixedUpdateRealTimeFactorが0以外だとAGXのステップがwall clockベースで実時間ペースに制限され、
        /// Time.timeがN倍で進むのに物理が1倍のままとなって非同期になるため、倍率適用時に設定を検証する。
        /// なお、AutoSteppingModeはControlPhysicsがポーズ機構として切り替えており、このタイミングでは
        /// Disabledが正常なためチェックしない。
        /// </summary>
        void ValidateSimulationSteppingSettings()
        {
            if (!Simulation.HasInstance)
                return;

            var simulation = Simulation.Instance;

            if (simulation.FixedUpdateRealTimeFactor != 0.0f)
                Debug.LogError(
                    $"Simulation speed multiplier requires AGXUnity Simulation.FixedUpdateRealTimeFactor == 0, " +
                    $"but it is {simulation.FixedUpdateRealTimeFactor}. AGX stepping will be throttled to real time " +
                    $"and desynchronize from game time. Set FixedUpdateRealTimeFactor to 0 in the Simulation inspector.");
        }




        void OnCameraSetClicked(List<Camera> cameras)
        {

            //var root = _uiDocument.rootVisualElement;
            var camera_root = _uiCameraSelectDocument.rootVisualElement;
            var toggles = camera_root.Query<Toggle>().ToList();

            int SelectedCameraCounter = 0;
            foreach (var toggle in toggles)
            {
                if (toggle.value == true)
                {
                    SelectedCameraCounter = SelectedCameraCounter + 1;
                }
            }

            if (SelectedCameraCounter > 3)
            {
                MessageDaialogUIobj = Instantiate(MessageDaialogUI);
                _uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

                var root = _uiMessageDaialogDocument.rootVisualElement;
                //root.Q<UnityEngine.UIElements.Label>("Title").text = "使用カメラ台数超過エラー";
                //root.Q<UnityEngine.UIElements.Label>("Message").text = "カメラが4台以上選択されています。";
                root.Q<UnityEngine.UIElements.Label>("Title").text = "Error";
                root.Q<UnityEngine.UIElements.Label>("Message").text = "More than four cameras are selected.";

                return;

            }
            else if (SelectedCameraCounter == 0)
            {
                MessageDaialogUIobj = Instantiate(MessageDaialogUI);
                _uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

                var root = _uiMessageDaialogDocument.rootVisualElement;
                //root.Q<UnityEngine.UIElements.Label>("Title").text = "使用カメラ未選択エラー";
                //root.Q<UnityEngine.UIElements.Label>("Message").text = "カメラが選択されていません。";
                root.Q<UnityEngine.UIElements.Label>("Title").text = "Error";
                root.Q<UnityEngine.UIElements.Label>("Message").text = "No camera selected.";
                return;

            }

            //Debug.Log(toggles.Count);
            //Debug.Log(cameras.Count);

            for (int i = 0; i < cameras.Count; i++)
            {
                if (toggles[i].value == true)
                {
                    cameras[i].gameObject.SetActive(true);
                    if (cameras[i].transform.parent != null)
                    {
                        if (cameras[i].transform.parent.gameObject.name.Contains("Str"))
                        {
                            cameras[i].transform.parent.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    cameras[i].gameObject.SetActive(false);
                    if (cameras[i].transform.parent != null)
                    {
                        if (cameras[i].transform.parent.gameObject.name.Contains("Str"))
                        {
                            cameras[i].transform.parent.gameObject.SetActive(false);
                        }
                    }
                }

                cameras[i].targetTexture = null;

            }


            _uiDocument.gameObject.SetActive(true);
            _uiCameraSelectDocument.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_uiCameraSelectDocument.gameObject);

            setupMenuUI();

            GlobalVariables.ForceCameraChange = true;
        }


        void CamerasInvisible()
        {

            List<Camera> cameras = new List<Camera>();
            //cameras.AddRange(FindObjectsOfType<Camera>(true));
            cameras.AddRange(CameraChanger.FindGameCameras(true));

            for (int i = 0; i < cameras.Count; i++)
            {

                if (cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_"))
                {
                    Renderer[] renders = cameras[i].gameObject.transform.root.gameObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer render in renders)
                    {
                        Material material = render.material;
                        material.SetFloat("_Mode", 2);
                        Color color = material.color;
                        color.a = 0.0f;
                        material.color = color;
                    }
                }
            }

        }

        void setupMenuUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Q<Button>("DunpTruckPositionSetting").clicked += () => OnDunpTruckPositionSettingClicked();
            root.Q<Button>("CameraPositionSetting").clicked += () => OnCameraPositionSettingClicked();
            root.Q<Button>("SelectCamera").clicked += () => OnSelectCameraClicked();
            root.Q<Button>("SimulationStart").clicked += () => OnSimulationStartClicked();

            var showScoreToggle = root.Q<Toggle>("ShowScore");
            if (showScoreToggle != null)
            {
                showScoreToggle.SetValueWithoutNotify(GlobalVariables.ShowScoreBoard);
                showScoreToggle.RegisterValueChangedCallback(evt =>
                {
                    GlobalVariables.ShowScoreBoard = evt.newValue;
                });
            }

            var simSpeedGroup = root.Q<RadioButtonGroup>("SimulationSpeed");
            if (simSpeedGroup != null)
            {
                simSpeedGroup.SetValueWithoutNotify(IndexFromMultiplier(GlobalVariables.SimulationSpeedMultiplier));
                simSpeedGroup.RegisterValueChangedCallback(evt =>
                {
                    GlobalVariables.SimulationSpeedMultiplier = MultiplierFromIndex(evt.newValue);
                });
            }
        }

        static readonly float[] SimSpeedChoices = { 1.0f, 1.5f, 2.0f, 3.0f, 4.0f };

        static float MultiplierFromIndex(int index)
        {
            if (index < 0 || index >= SimSpeedChoices.Length) return 1.0f;
            return SimSpeedChoices[index];
        }

        static int IndexFromMultiplier(float multiplier)
        {
            for (int i = 0; i < SimSpeedChoices.Length; i++)
                if (Mathf.Approximately(SimSpeedChoices[i], multiplier)) return i;
            return 0;
        }


        public void GenerateCheckboxes(UIDocument uiDocument, int numberOfCheckboxes, List<Camera> cameras)
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                Debug.LogError("UIDocument");
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;



            for (int i = 0; i < numberOfCheckboxes; i++)
            {
                VisualElement checkboxContainer = new VisualElement();
                //checkboxContainer.style.flexDirection = FlexDirection.Row;
                checkboxContainer.style.flexDirection = FlexDirection.Column;
                checkboxContainer.style.alignItems = Align.FlexStart;

                Toggle checkbox;

                string camName = cameras[i].transform.root.gameObject.name;
                if (camName.Contains("Str")) camName = cameras[i].transform.gameObject.name;

                //UnityEngine.UIElements.Label label = new UnityEngine.UIElements.Label(cameras[i].transform.root.gameObject.name);
                checkbox = new Toggle();

                //checkbox.label = "";
                checkbox.label = camName;
                checkbox.name = $"checkbox_{i + 1}";
                checkbox.value = false;
                checkbox.style.position = Position.Relative;
                checkbox.style.marginBottom = 4;

                checkbox.RegisterValueChangedCallback(evt =>
                {

                });

                checkboxContainer.Add(checkbox);
                //checkboxContainer.Add(label);
                root.Q<VisualElement>("CameraList").Add(checkboxContainer);
            }
        }


        IEnumerator SaveCoroutine(saveScript savescript)
        {
            Debug.Log("GlobalVariables.ConfirmWaitFlag: " + GlobalVariables.ConfirmWaitFlag);
            Debug.Log("completedFlag: " + savescript.completedFlag);

            yield return new WaitUntil(() => (savescript.completedFlag == true));

            if (savescript.completedFlag)
            {
                Debug.Log("savescript");

                MessageDaialogUIobj = Instantiate(MessageDaialogUI);
                _uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

                var root = _uiMessageDaialogDocument.rootVisualElement;
                root.Q<UnityEngine.UIElements.Label>("Title").text = "Information";
                root.Q<UnityEngine.UIElements.Label>("Message").text = "Save completed.";

                savescript.completedFlag = false;

                // タイマー再開
                CountdownTimer.isRunning = true;
            }
        }

        void SaveClicked()
        {
            Debug.Log("SaveClicked!");

            saveScript savescript = null;

            // 地形を保存
            if (GlobalVariables.ActionMode == 3)
            {
                // タイマー停止
                CountdownTimer.isRunning = false;

                savescript = SaveLoadUIobj.GetComponent<saveScript>();
                savescript.OnClick();
            }
            else
            {
                savescript = SaveLoadSettingUIobj.GetComponent<saveScript>();
                savescript.OnClick();
            }

            // コルーチンの実行  
            StartCoroutine(SaveCoroutine(savescript));
        }


        IEnumerator LoadCoroutine(loadScript loadscript)
        {
            Debug.Log("GlobalVariables.ConfirmWaitFlag: " + GlobalVariables.ConfirmWaitFlag);
            Debug.Log("completedFlag: " + loadscript.completedFlag);

            yield return new WaitUntil(() => (loadscript.completedFlag == true));

            if (loadscript.completedFlag)
            {
                Debug.Log("loadscript");

                //MessageDaialogUIobj = Instantiate(MessageDaialogUI);
                //_uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

                //var root = _uiMessageDaialogDocument.rootVisualElement;
                //root.Q<UnityEngine.UIElements.Label>("Title").text = "Information";
                //root.Q<UnityEngine.UIElements.Label>("Message").text = "Load completed.";

                loadscript.completedFlag = false;
            }
        }

        void LoadClicked()
        {
            Debug.Log("LoadClicked!");

            loadScript loadscript = null;

            // 地形を読込
            if (GlobalVariables.ActionMode == 3)
            {
                // タイマー停止
                CountdownTimer.isRunning = false;

                loadscript = SaveLoadUIobj.GetComponent<loadScript>();
                loadscript.OnClick();
            }
            else
            {
                loadscript = SaveLoadSettingUIobj.GetComponent<loadScript>();
                loadscript.OnClick();
            }

            // コルーチンの実行  
            StartCoroutine(LoadCoroutine(loadscript));
        }


        IEnumerator ResetCompleted()
        {
            yield return new WaitUntil(() => (GlobalVariables.SelectMode == -1));

            MessageDaialogUIobj = Instantiate(MessageDaialogUI);
            _uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

            var root = _uiMessageDaialogDocument.rootVisualElement;
            root.Q<UnityEngine.UIElements.Label>("Title").text = "Information";
            root.Q<UnityEngine.UIElements.Label>("Message").text = "Reset completed.";
        }

        IEnumerator ResetCoroutine()
        {
            Debug.Log("GlobalVariables.ConfirmWaitFlag: " + GlobalVariables.ConfirmWaitFlag);
            yield return new WaitUntil(() => (GlobalVariables.ConfirmWaitFlag != 1));


            if (GlobalVariables.ConfirmWaitFlag == 2)
            {
                Debug.Log("Start!");

                // 別のスクリプトで地形と重機の読み込みを実行するためフラグ切り替え
                GlobalVariables.SelectMode = 2;


                List<Camera> cameras = new List<Camera>();
                cameras.AddRange(FindObjectsOfType<Camera>(true));

                for (int i = 0; i < cameras.Count; i++)
                {
                    //Debug.Log(cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_"));

                    // 追加したカメラのみ削除
                    if (cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_"))
                    {
                        UnityEngine.Object.Destroy(cameras[i].gameObject.transform.root.gameObject);
                    }
                }

                // カメラを削除
                GlobalVariables.CameraCounter = 0;


                if (GlobalVariables.ActionMode == 3)
                {
                    // タイマー停止
                    CountdownTimer.isRunning = false;

                    // スコアリセット
                    GlobalVariables.score = 0;

                    // 速度倍率を戻す
                    Time.timeScale = 1.0f;

                    // イベントログ表示をクリア
                    if (EventLogUIobj != null)
                    {
                        var eventLog = EventLogUIobj.GetComponent<EventLogUI>();
                        if (eventLog != null) eventLog.Clear();
                    }


                    // 設定画面に戻る
                    StatusBoardUIobj = Instantiate(StatusBoardUI);
                    sBoard = StatusBoardUIobj.GetComponent<StatusBoard>();
                    sBoard.SetStatusMessage("Start!!");
                    sBoard.SetMessageColor(Color.blue);

                    munuUIobj = Instantiate(MenuUI);
                    _uiDocument = munuUIobj.GetComponent<UIDocument>();
                    var root = _uiDocument.rootVisualElement;

                    setupMenuUI();

                    ObjectTransRotUIobj = null;


                    // セーブ・ロード・リセット機能
                    SaveLoadSettingUIobj = Instantiate(SaveLoadSettingUI);

                    _uiSaveLoadSettingDocument = SaveLoadSettingUIobj.GetComponent<UIDocument>();
                    _uiSaveLoadSettingDocument.gameObject.SetActive(true);

                    var save_root = _uiSaveLoadSettingDocument.rootVisualElement;

                    save_root.Q<Button>("Save").clicked += () => SaveClicked();
                    save_root.Q<Button>("Load").clicked += () => LoadClicked();
                    save_root.Q<Button>("Reset").clicked += () => ResetClicked();


                    // タイマーやスコアを非表示にする
                    CountdownTimerUIobj.SetActive(false);
                    ScoreUIobj.SetActive(false);
                    _uiSaveLoadDocument.gameObject.SetActive(false);


                    // モードをセット
                    GlobalVariables.changeActionMode(-1);
                }

                GlobalVariables.ConfirmWaitFlag = 0;


                // コルーチンの実行  
                StartCoroutine(ResetCompleted());
            }
        }

        void ResetClicked()
        {
            Debug.Log("ResetClicked!");

            if (GlobalVariables.ConfirmWaitFlag == 0)
            {
                GlobalVariables.ConfirmWaitFlag = 1;

                // ダイアログ表示
                ConfirmDaialogUIobj = Instantiate(ConfirmDaialogUI);
                _uiConfirmDaialogDocument = ConfirmDaialogUIobj.GetComponent<UIDocument>();

                var _root = _uiConfirmDaialogDocument.rootVisualElement;
                _root.Q<UnityEngine.UIElements.Label>("Title").text = "Confirm";
                _root.Q<UnityEngine.UIElements.Label>("Message").text = "Reset. Are you sure?";


                // コルーチンの実行  
                StartCoroutine(ResetCoroutine());
            }
        }


        void MainCameraSettingsClicked() {
            GlobalVariables.SetMoveType = 3;

            _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();
            var trans_root = _uiObjectTransRotDocument.rootVisualElement;

            SetButtonColorSelect(trans_root.Q<Button>("MainCameraSettings"));
            SetButtonColorDefault(trans_root.Q<Button>("Translation"));
            SetButtonColorDefault(trans_root.Q<Button>("Rotation"));
            SetButtonColorDefault(trans_root.Q<Button>("Delete"));
        }

        void ObjTranslationClicked() {
            GlobalVariables.SetMoveType = 0;

            _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();
            var trans_root = _uiObjectTransRotDocument.rootVisualElement;

            SetButtonColorDefault(trans_root.Q<Button>("MainCameraSettings"));
            SetButtonColorSelect(trans_root.Q<Button>("Translation"));
            SetButtonColorDefault(trans_root.Q<Button>("Rotation"));
            SetButtonColorDefault(trans_root.Q<Button>("Delete"));
        }

        void ObjRotationClicked() {
            GlobalVariables.SetMoveType = 1;

            _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();
            var trans_root = _uiObjectTransRotDocument.rootVisualElement;

            SetButtonColorDefault(trans_root.Q<Button>("MainCameraSettings"));
            SetButtonColorDefault(trans_root.Q<Button>("Translation"));
            SetButtonColorSelect(trans_root.Q<Button>("Rotation"));
            SetButtonColorDefault(trans_root.Q<Button>("Delete"));
        }

        void ObjDeleteClicked()
        {
            GlobalVariables.SetMoveType = 2;

            _uiObjectTransRotDocument = ObjectTransRotUIobj.GetComponent<UIDocument>();
            var trans_root = _uiObjectTransRotDocument.rootVisualElement;

            SetButtonColorDefault(trans_root.Q<Button>("MainCameraSettings"));
            SetButtonColorDefault(trans_root.Q<Button>("Translation"));
            SetButtonColorDefault(trans_root.Q<Button>("Rotation"));
            SetButtonColorSelect(trans_root.Q<Button>("Delete"));
        }


        void SetButtonColorSelect(Button btn)
        {
            // 文字色
            btn.style.color = Color.white;
            // 背景色
            btn.style.backgroundColor = new StyleColor(new Color32(0, 58, 118, 255));
        }

        void SetButtonColorDefault(Button btn)
        {
            // 文字色
            btn.style.color = Color.black;
            // 背景色
            btn.style.backgroundColor = new StyleColor(new Color32(188, 188, 188, 255));
        }

        void SetButtonAddFunction(UIDocument doc)
        {
            var trans_root = doc.rootVisualElement;

            trans_root.Q<Button>("MainCameraSettings").clicked += () => MainCameraSettingsClicked();
            trans_root.Q<Button>("Translation").clicked += () => ObjTranslationClicked();
            trans_root.Q<Button>("Rotation").clicked += () => ObjRotationClicked();
            trans_root.Q<Button>("Delete").clicked += () => ObjDeleteClicked();


            if (GlobalVariables.SetMoveType == 0)
            {
                ObjTranslationClicked();
            }
            else if (GlobalVariables.SetMoveType == 1)
            {
                ObjRotationClicked();
            }
            else if (GlobalVariables.SetMoveType == 2)
            {
                ObjDeleteClicked();
            }
            else if (GlobalVariables.SetMoveType == 3)
            {
                MainCameraSettingsClicked();
            }
        }



        IEnumerator EndCoroutine()
        {
            Debug.Log("GlobalVariables.ConfirmWaitFlag: " + GlobalVariables.ConfirmWaitFlag);
            yield return new WaitUntil(() => (GlobalVariables.ConfirmWaitFlag != 1));

            if (GlobalVariables.ConfirmWaitFlag == 2)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                //Application.Quit();
                UnityEngine.Application.Quit();
#endif

                GlobalVariables.ConfirmWaitFlag = 0;
            }
        }


        private void EndGame()
        {
            //Escが押されたらプログラム終了
            if (Input.GetKey(KeyCode.Escape))
            {
                Debug.Log("EscapeClicked!");

                if (GlobalVariables.ConfirmWaitFlag == 0)
                {
                    GlobalVariables.ConfirmWaitFlag = 1;

                    // ダイアログ表示
                    ConfirmDaialogUIobj = Instantiate(ConfirmDaialogUI);
                    _uiConfirmDaialogDocument = ConfirmDaialogUIobj.GetComponent<UIDocument>();

                    var _root = _uiConfirmDaialogDocument.rootVisualElement;
                    _root.Q<UnityEngine.UIElements.Label>("Title").text = "Confirm";
                    _root.Q<UnityEngine.UIElements.Label>("Message").text = "Exiting. Are you sure?";


                    // コルーチンの実行  
                    StartCoroutine(EndCoroutine());
                }
            }

        }

    }
}
