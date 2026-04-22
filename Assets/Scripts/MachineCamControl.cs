using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;

namespace PWRISimulator
{
    /// <summary>
    /// 重機に搭載されているカメラの制御処理
    /// </summary>
    public class MachineCamControl : MonoBehaviour
    {
        private GameObject machineObj;

        private Slider HorizontalSlider;
        private Slider VerticalSlider;
        private Slider UpDownSlider;
        private Slider FrontRearSlider;
        private Slider LeftRightSlider;

        private float VerticalAngle = 0.0f;
        private float HorizontalAngle = 0.0f;

        private float UpDownPos = 0.0f;
        private float FrontRearPos = 0.0f;
        private float LeftRightPos = 0.0f;

        private Transform obj;
        private float baseLocalX = 0.0f;

        private float HorizontalSliderLastVal = 0.0f;
        private float VerticalSliderLastVal = 0.0f;
        private float UpDownSliderLastVal = 0.0f;
        private float FrontRearSliderLastVal = 0.0f;
        private float LeftRightSliderLastVal = 0.0f;

        private float timer = 0f;

        private void HorizontalSliderOnValueChanged(ChangeEvent<float> evt){

            if (Mathf.Approximately(evt.newValue, HorizontalSliderLastVal)) return;
            //UnityEngine.Debug.Log(evt.newValue);
            //var angls = obj.transform.localRotation;
            //UnityEngine.Debug.Log(angls);
            HorizontalAngle = evt.newValue;
            //bj.transform.localRotation = Quaternion.Euler(VerticalAngle, HorizontalAngle, angls.z);

            HorizontalSliderLastVal = evt.newValue;
            SaveToGlobal();
        }


        private void VerticalSliderOnValueChanged(ChangeEvent<float> evt)
        {

            if (Mathf.Approximately(evt.newValue, VerticalSliderLastVal)) return;
            //UnityEngine.Debug.Log(evt.newValue);
            //var angls = obj.transform.localRotation;
            //UnityEngine.Debug.Log(angls);
            VerticalAngle = evt.newValue;
            //obj.transform.localRotation = Quaternion.Euler(VerticalAngle, HorizontalAngle, angls.z);

            VerticalSliderLastVal = evt.newValue;
            SaveToGlobal();
        }


        private void UpDownSliderOnValueChanged(ChangeEvent<float> evt)
        {

            if (Mathf.Approximately(evt.newValue, UpDownSliderLastVal)) return;
            //UnityEngine.Debug.Log(evt.newValue);
            //var pos = obj.transform.localPosition;
            //UnityEngine.Debug.Log(pos);
            UpDownPos = evt.newValue;
            //obj.transform.localPosition = new Vector3(pos.x, 2.4f + evt.newValue, pos.z);

            UpDownSliderLastVal = evt.newValue;
            SaveToGlobal();
        }

        private void FrontRearSliderOnValueChanged(ChangeEvent<float> evt)
        {

            if (Mathf.Approximately(evt.newValue, FrontRearSliderLastVal)) return;
            //UnityEngine.Debug.Log(evt.newValue);
            //var pos = obj.transform.localPosition;
            //UnityEngine.Debug.Log(pos);
            FrontRearPos = evt.newValue;
            //obj.transform.localPosition = new Vector3(pos.x, pos.y, 2.3f - evt.newValue);

            FrontRearSliderLastVal = evt.newValue;
            SaveToGlobal();
        }

        private void LeftRightSliderOnValueChanged(ChangeEvent<float> evt)
        {

            if (Mathf.Approximately(evt.newValue, LeftRightSliderLastVal)) return;
            LeftRightPos = evt.newValue;
            LeftRightSliderLastVal = evt.newValue;
            SaveToGlobal();
        }

        private void SaveToGlobal()
        {
            if (machineObj == null) return;
            GlobalVariables.MachineCameraSliders[machineObj.name] = new saveScript.MachineCameraSliderState
            {
                machineName = machineObj.name,
                horizontalAngle = HorizontalAngle,
                verticalAngle = VerticalAngle,
                upDownPos = UpDownPos,
                frontRearPos = FrontRearPos,
                leftRightPos = LeftRightPos,
            };
        }


        public void Initialize(GameObject machineObject)
        {
            this.machineObj = machineObject;
            UnityEngine.Debug.Log("Initilize : " + this.machineObj.name);

            obj = machineObj.transform.Find("base_link/body_link/CameraStr")
                ?? machineObj.transform.Find("base_link/track_link/CameraStr");

            if (obj == null)
            {
                UnityEngine.Debug.Log("Object NULL");
            }
            else
            {
                UnityEngine.Debug.Log("Object Not NULL");
                baseLocalX = obj.localPosition.x;
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            HorizontalSlider = root.Q<Slider>("Horizontal");
            VerticalSlider = root.Q<Slider>("Vertical");
            UpDownSlider = root.Q<Slider>("UpDown");
            FrontRearSlider = root.Q<Slider>("FrontRear");
            LeftRightSlider = root.Q<Slider>("LeftRight");

            if (GlobalVariables.MachineCameraSliders.TryGetValue(machineObj.name, out var saved))
            {
                HorizontalAngle = saved.horizontalAngle;
                VerticalAngle = saved.verticalAngle;
                UpDownPos = saved.upDownPos;
                FrontRearPos = saved.frontRearPos;
                LeftRightPos = saved.leftRightPos;

                HorizontalSliderLastVal = HorizontalAngle;
                VerticalSliderLastVal = VerticalAngle;
                UpDownSliderLastVal = UpDownPos;
                FrontRearSliderLastVal = FrontRearPos;
                LeftRightSliderLastVal = LeftRightPos;

                HorizontalSlider.SetValueWithoutNotify(HorizontalAngle);
                VerticalSlider.SetValueWithoutNotify(VerticalAngle);
                UpDownSlider.SetValueWithoutNotify(UpDownPos);
                FrontRearSlider.SetValueWithoutNotify(FrontRearPos);
                LeftRightSlider.SetValueWithoutNotify(LeftRightPos);
            }


            HorizontalSlider.UnregisterValueChangedCallback(HorizontalSliderOnValueChanged);
            HorizontalSlider.RegisterValueChangedCallback(HorizontalSliderOnValueChanged);

            VerticalSlider.UnregisterValueChangedCallback(VerticalSliderOnValueChanged);
            VerticalSlider.RegisterValueChangedCallback(VerticalSliderOnValueChanged);

            UpDownSlider.UnregisterValueChangedCallback(UpDownSliderOnValueChanged);
            UpDownSlider.RegisterValueChangedCallback(UpDownSliderOnValueChanged);

            FrontRearSlider.UnregisterValueChangedCallback(FrontRearSliderOnValueChanged);
            FrontRearSlider.RegisterValueChangedCallback(FrontRearSliderOnValueChanged);

            LeftRightSlider.UnregisterValueChangedCallback(LeftRightSliderOnValueChanged);
            LeftRightSlider.RegisterValueChangedCallback(LeftRightSliderOnValueChanged);

        }

        public void ClearCallBack()
        {
            HorizontalSlider.UnregisterValueChangedCallback(HorizontalSliderOnValueChanged);
            VerticalSlider.UnregisterValueChangedCallback(VerticalSliderOnValueChanged);
            UpDownSlider.UnregisterValueChangedCallback(UpDownSliderOnValueChanged);
            FrontRearSlider.UnregisterValueChangedCallback(FrontRearSliderOnValueChanged);
            LeftRightSlider.UnregisterValueChangedCallback(LeftRightSliderOnValueChanged);

        }


        // Start is called before the first frame update
        void Start()
        {



        }

        // Update is called once per frame
        void Update()
        {
            if (obj != null && obj.gameObject.name == "CameraStr") {
                var angls = obj.transform.localRotation;
                obj.transform.localRotation = Quaternion.Euler(VerticalAngle, HorizontalAngle, angls.z);
                var pos = obj.transform.localPosition;
                obj.transform.localPosition = new Vector3(baseLocalX + LeftRightPos, 2.4f + UpDownPos, 2.3f - FrontRearPos);
            }
        }
    }
}
