using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;

namespace PWRISimulator
{
    /// <summary>
    /// èdã@Ç…ìãç⁄Ç≥ÇÍÇƒÇ¢ÇÈÉJÉÅÉâÇÃêßå‰èàóù
    /// </summary>
    public class MachineCamControl : MonoBehaviour
    {
        private GameObject machineObj;

        private Slider HorizontalSlider;
        private Slider VerticalSlider;
        private Slider UpDownSlider;
        private Slider FrontRearSlider;

        private float VerticalAngle = 0.0f;
        private float HorizontalAngle = 0.0f;

        private float UpDownPos = 0.0f;
        private float FrontRearPos = 0.0f;

        private Transform obj;

        private float HorizontalSliderLastVal = 0.0f;
        private float VerticalSliderLastVal = 0.0f;
        private float UpDownSliderLastVal = 0.0f;
        private float FrontRearSliderLastVal = 0.0f;

        private float timer = 0f;

        private void HorizontalSliderOnValueChanged(ChangeEvent<float> evt){

            if (Mathf.Approximately(evt.newValue, HorizontalSliderLastVal)) return;
            //UnityEngine.Debug.Log(evt.newValue);
            //var angls = obj.transform.localRotation;
            //UnityEngine.Debug.Log(angls);
            HorizontalAngle = evt.newValue;
            //bj.transform.localRotation = Quaternion.Euler(VerticalAngle, HorizontalAngle, angls.z);

            HorizontalSliderLastVal = evt.newValue;
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
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            HorizontalSlider = root.Q<Slider>("Horizontal");
            VerticalSlider = root.Q<Slider>("Vertical");
            UpDownSlider = root.Q<Slider>("UpDown");
            FrontRearSlider = root.Q<Slider>("FrontRear");



            HorizontalSlider.UnregisterValueChangedCallback(HorizontalSliderOnValueChanged);
            HorizontalSlider.RegisterValueChangedCallback(HorizontalSliderOnValueChanged);

            VerticalSlider.UnregisterValueChangedCallback(VerticalSliderOnValueChanged);
            VerticalSlider.RegisterValueChangedCallback(VerticalSliderOnValueChanged);

            UpDownSlider.UnregisterValueChangedCallback(UpDownSliderOnValueChanged);
            UpDownSlider.RegisterValueChangedCallback(UpDownSliderOnValueChanged);

            FrontRearSlider.UnregisterValueChangedCallback(FrontRearSliderOnValueChanged);
            FrontRearSlider.RegisterValueChangedCallback(FrontRearSliderOnValueChanged);

        }

        public void ClearCallBack()
        {
            HorizontalSlider.UnregisterValueChangedCallback(HorizontalSliderOnValueChanged);
            VerticalSlider.UnregisterValueChangedCallback(VerticalSliderOnValueChanged);
            UpDownSlider.UnregisterValueChangedCallback(UpDownSliderOnValueChanged);
            FrontRearSlider.UnregisterValueChangedCallback(FrontRearSliderOnValueChanged);

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
                obj.transform.localPosition = new Vector3(pos.x, 2.4f + UpDownPos, 2.3f - FrontRearPos);
            }
        }
    }
}
