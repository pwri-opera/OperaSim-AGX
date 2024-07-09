using System;
using AGXUnity;
using UnityEngine;
using UnityEngine.Events;

namespace PWRISimulator
{
    public class DumpDoorLock : MonoBehaviour
    {
        [SerializeField] private LockController backDoorJoint;
        [SerializeField] private GameObject containerBody;
        [Range(0, 90)]
        public float angleThreshold = 0.1f;
        public bool printToConsole = true;
        public UnityEvent lockEvent;
        public UnityEvent unlockEvent;

        public bool isLocked { get { return backDoorJoint != null ? backDoorJoint.Enable : false; } }

        private void Update()
        {
            UpdateDoorLock();
        }

        private void OnDisable()
        {
            SetLock(false);
        }

        private void UpdateDoorLock()
        {
            if (containerBody == null)
                return;

            var rotationValue = containerBody.transform.localEulerAngles.y;

            // 角度値が0から360へ飛ばないように範囲を[0, 360]から[-180、180]へ変換
            if (rotationValue > 180)
                rotationValue -= 360;
            else if (rotationValue < -180)
                rotationValue += 360;

            bool needsLock = rotationValue <= angleThreshold;
            SetLock(needsLock);
        }

        void SetLock(bool lockEnabled)
        {
            if (backDoorJoint.GetInitialized<LockController>() == null)
                return;

            if (lockEnabled == backDoorJoint.Enable)
                return;
            
            //　AgxのLockControllerをEnable/Disable
            backDoorJoint.Enable = lockEnabled;

            if (printToConsole)
            {
                string lockedMsg = lockEnabled ? "Locked" : "Unlocked";
                Debug.Log($"{name} : {lockedMsg} door ({backDoorJoint.name})");
            }

            // InspectorのGUIから設定したEventを呼び出す
            if (lockEnabled && lockEvent != null)
                lockEvent.Invoke();
            else if (!lockEnabled && unlockEvent != null)
                unlockEvent.Invoke();
        }
    }
}