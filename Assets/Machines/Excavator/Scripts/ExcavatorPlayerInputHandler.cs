using UnityEngine;
using UnityEngine.InputSystem;

namespace PWRISimulator
{
    /// <summary>
    /// Excavator用のInputActionMapのActionsをExcavator.csのコンストレイントインタフェースと繋ぐスクリプト。
    /// 
    /// UnityのPlayerInputコンポネントを同じGameObjectに挿入し、InputActionAsset及びDefault Mapプロパティを設定し、
    /// BehaviourプロパティをSendMessagesに設定する必要。すると、以下のOnXXXというメソッドが対してXXXというActionが発生すると
    /// 自動的に呼ばれる。
    /// </summary>
    public class ExcavatorPlayerInputHandler : MonoBehaviour
    {
        /// <summary>
        /// 対象のExcvatorコンポネント。
        /// </summary>
        public Excavator excavator;

        public bool printDebugMessages = false;

        public void Start()
        {
            if (excavator != null)
            {
                SetExcavatorConstraintVelocityControl(excavator.leftSprocket);
                SetExcavatorConstraintVelocityControl(excavator.rightSprocket);
                SetExcavatorConstraintVelocityControl(excavator.swing);
                SetExcavatorConstraintVelocityControl(excavator.boomTilt);
                SetExcavatorConstraintVelocityControl(excavator.armTilt);
                SetExcavatorConstraintVelocityControl(excavator.bucketTilt);
            }
        }

        public void OnLeftSprocket(InputValue value)
        {
            SetExcavatorInputValue(excavator?.leftSprocket, value.Get<float>());
        }

        public void OnRightSprocket(InputValue value)
        {
            SetExcavatorInputValue(excavator?.rightSprocket, value.Get<float>());
        }

        public void OnSwing(InputValue value)
        {
            SetExcavatorInputValue(excavator?.swing, value.Get<float>());
        }

        public void OnBoomTilt(InputValue value)
        {
            SetExcavatorInputValue(excavator?.boomTilt, value.Get<float>());
        }

        public void OnArmTilt(InputValue value)
        {
            SetExcavatorInputValue(excavator?.armTilt, value.Get<float>());
        }

        public void OnBucketTilt(InputValue value)
        {
            SetExcavatorInputValue(excavator?.bucketTilt, value.Get<float>());
        }
        
        protected void SetExcavatorInputValue(ConstraintControl constraintControl, double value)
        {
            if (constraintControl != null)
            {
                if (printDebugMessages)
                    Debug.Log($"{constraintControl.constraint.name} input value = {value}");

                constraintControl.controlValue = value;
            }
        }

        protected void SetExcavatorConstraintVelocityControl(ConstraintControl constraintControl)
        {
            if (constraintControl != null)
                constraintControl.controlType = ControlType.Speed;
        }
    }
}