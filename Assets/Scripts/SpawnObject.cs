using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Debug = UnityEngine.Debug;

using UnityEngine.UIElements;

namespace PWRISimulator
{
    /// <summary>
    /// d‹@“™‚Ì¶¬ˆ—
    /// </summary>

    //[RequireComponent(typeof(Camera))]
    public class SpawnObject : MonoBehaviour
    {

        [SerializeField] GameObject MessageDaialogUI;
        private GameObject MessageDaialogUIobj;
        private UIDocument _uiMessageDaialogDocument;


        public const string ic120_path = "Prefabs/ic120_prefVar";
        public const string zx120_path = "Prefabs/zx120_prefVar";
        public const string zx200_path = "Prefabs/zx200_prefVar";
        public const string camera_path = "Prefabs/CameraObj_prefVariant";

        public const string ic120_objName = "ic120_prefVar";
        public const string zx120_objName = "zx120_prefVar";
        public const string zx200_objName = "zx200_prefVar";

        private Vector3 mousePosition;

        public Camera myCamera { get; private set; }
        public List<Camera> cameras = new List<Camera>();

        public float deltaTime = 1.0f;


        private LayerMask selectionMask = Physics.DefaultRaycastLayers;

        private Mouse mouse;

        //private int ic120Counter = 0;
        //private int CameraCounter = 0;

        private ic120obj ic120obj;
        private cameraObj cameraObj;



        void Awake()
        {
            myCamera = GetComponent<Camera>();

        }


            // Start is called before the first frame update
            void Start()
        {
            UnityEngine.Cursor.visible = true;
            //Screen.lockCursor = false;        //old

            mouse = Mouse.current;
            if (mouse == null)
            {
                //Debug.Log(mouse);
                InputSystem.EnableDevice(mouse);
            }

            ic120obj = new ic120obj();
            cameraObj = new cameraObj();

        }

        // Update is called once per frame
        void Update()
        {

            if (GlobalVariables.ActionMode < 0)
            {
                return;
            }



            if (mouse.middleButton.wasReleasedThisFrame)
            {
                Debug.Log("Click");

                cameras.Clear();

                //Œ»İ—LŒø‚ÈƒJƒƒ‰æ“¾
                //cameras.AddRange(FindObjectsOfType<Camera>());
                cameras.AddRange(CameraChanger.FindGameCameras());
                for (int i = 0; i < cameras.Count; i++)
                {
                    if (cameras[i].enabled == true && cameras[i].gameObject.name == "Main Camera")
                    {
                        myCamera = cameras[i];
                    }
                }

                UnityEngine.Debug.Log(myCamera.gameObject.name);

                RaycastHit hitInfo;

                Vector2 mouseP = Mouse.current.position.ReadValue();
                Ray ray = myCamera.ScreenPointToRay(mouseP);

                if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, selectionMask))
                {
                    Debug.Log("Hit");

                    Vector3 mousePosition = hitInfo.point;
                    mousePosition.y = 0.5f + mousePosition.y;
                    Vector3 worldPosition = myCamera.ScreenToWorldPoint(mousePosition);
                    Vector3 mousePosition_ = myCamera.ScreenToWorldPoint(mouse.position.ReadValue());

                    //Debug.Log(Input.mousePosition);
                    Debug.Log(myCamera.ScreenToWorldPoint(mouse.position.ReadValue()));
                    Debug.Log(mousePosition);
                    Debug.Log(worldPosition);


                    if (GlobalVariables.ActionMode == 0) {

                        // ã‚·ãƒ¼ãƒ³ã«åŸ‹ã‚è¾¼ã¾ã‚ŒãŸ ic120_0 ã¯ã‚«ã‚¦ãƒ³ã‚¿ã«å«ã¾ã‚Œãªã„ãŸã‚ã€
                        // å­˜åœ¨ã™ã‚‹ã‚ªãƒ–ã‚¸ã‚§ã‚¯ãƒˆã®ç©ºãç•ªå·ã§ä¸Šé™ã‚’åˆ¤å®šã™ã‚‹ (#116)
                        int objID = findSpawnObjID("ic120_", GlobalVariables.MaxDunpTracks);

                        if (objID >= 0)
                        {
                            ic120obj.Spawn_ic120(mousePosition, Quaternion.identity, objID, ic120_path);
                            GlobalVariables.ic120Counter = GlobalVariables.ic120Counter + 1;
                            //GameObject.Find(ic120_pref.name + "/base_link/track_link").SetActive(false);
                            Debug.Log("ic120 Spawn");
                        }
                        else
                        {
                            MessageDaialogUIobj = Instantiate(MessageDaialogUI);
                            _uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

                            var root = _uiMessageDaialogDocument.rootVisualElement;
                            //root.Q<UnityEngine.UIElements.Label>("Title").text = "İ’u‰Â”\ƒgƒ‰ƒbƒN‘ä”’´‰ßƒGƒ‰[";
                            //root.Q<UnityEngine.UIElements.Label>("Message").text = "Šù‚Éƒgƒ‰ƒbƒN‚ª" + GlobalVariables.MaxDunpTracks.ToString() + "‘äİ’u‚³‚ê‚Ä‚¢‚Ü‚·B";

                            root.Q<UnityEngine.UIElements.Label>("Title").text = "Error";
                            root.Q<UnityEngine.UIElements.Label>("Message").text = "There are already " + GlobalVariables.MaxDunpTracks.ToString() + " trucks installed.";
                        }

                    }
                    else if (GlobalVariables.ActionMode == 1) {

                        int objID = findSpawnObjID("Camera_", GlobalVariables.MaxCameras);

                        if (objID >= 0)
                        {
                            cameraObj.Spawn_Camera(mousePosition, Quaternion.identity, objID, camera_path);
                            GlobalVariables.CameraCounter = GlobalVariables.CameraCounter + 1;
                            Debug.Log("Camera Spawn");
                        }
                        else
                        {
                            MessageDaialogUIobj = Instantiate(MessageDaialogUI);
                            _uiMessageDaialogDocument = MessageDaialogUIobj.GetComponent<UIDocument>();

                            var root = _uiMessageDaialogDocument.rootVisualElement;
                            //root.Q<UnityEngine.UIElements.Label>("Title").text = "İ’u‰Â”\ƒJƒƒ‰‘ä”’´‰ßƒGƒ‰[";
                            //root.Q<UnityEngine.UIElements.Label>("Message").text = "Šù‚ÉƒJƒƒ‰‚ª" + GlobalVariables.MaxCameras.ToString() + "‘äİ’u‚³‚ê‚Ä‚¢‚Ü‚·B";

                            root.Q<UnityEngine.UIElements.Label>("Title").text = "Error";
                            root.Q<UnityEngine.UIElements.Label>("Message").text = "There are already " + GlobalVariables.MaxCameras.ToString() + " cameras installed.";
                        }
                    }


                    //Debug.Log("Spawn");

                    //float deltaTime = Time.fixedDeltaTime;
                    //Physics.Simulate(deltaTime);
                    //}

                }

            }





        }

        /// <summary>
        /// ObjeName + ç•ªå· (0 ã€œ maxNum-1) ã®ã†ã¡ã€ã‚·ãƒ¼ãƒ³ã«å­˜åœ¨ã—ãªã„æœ€å°ã®ç•ªå·ã‚’è¿”ã™ã€‚
        /// ã™ã¹ã¦ä½¿ã‚ã‚Œã¦ã„ã‚Œã° -1 ã‚’è¿”ã™ã€‚
        /// </summary>
        int findSpawnObjID(String ObjeName, int maxNum)
        {
            for (int i = 0; i < maxNum; i++)
            {
                GameObject obj = GameObject.Find(ObjeName + i.ToString());
                if (obj == null) {
                    return i;
                }
            }
            return -1;
        }
    }
}
