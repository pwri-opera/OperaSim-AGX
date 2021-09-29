using System;
using UnityEngine;
using AGXUnity;
using AGXUnity.Utils;

namespace PWRISimulator
{
    /// <summary>
    /// より簡単なインタフェースでConstraintを制御したり、力などを実測したりできるようなConsraintプロクシクラス。
    /// </summary>
    [Serializable]
    public class ConstraintControl
    {
        #region Public

        /// <summary>
        /// 対象のConstraint。
        /// </summary>
        [InspectorLabel("Target Constraint")]
        public Constraint constraint;

        /// <summary>
        /// コンストレイントを制御するか。trueの場合は、controlTypeによるコンストレイントのTargetSpeedControllerか
        /// LockControllerを制御する。falseの場合は、対象のConstraintの設定を触らない。Playしている間に変更することができない。
        /// </summary>
        public bool controlEnabled = false;

        /// <summary>
        /// controlValue、つまり制御の指令値、の種類。初期化した後に変更することができない。
        /// </summary>
        /// <seealso cref="ControlType"/>
        [ConditionalHide("controlEnabled", true)]
        public ControlType controlType = ControlType.Speed;

        /// <summary>
        /// Constraint制御の指令値。controlTypeによって位置・角度か、速度・角速度か、力・トルク。
        /// </summary>
        [ConditionalHide("controlEnabled", true)]
        public double controlValue = 0.0f;

        /// <summary>
        /// Constraintの制御方法がRigidBodyにかけられる最大の力／トルク。
        /// </summary>
        [ConditionalHide("controlEnabled", true)]
        public double controlMaxForce = double.PositiveInfinity;

        public double currentPosition
        {
            get { return nativeConstraint != null ? nativeConstraint.getAngle() : 0.0; }
        }
        public double currentSpeed
        {
            get { return nativeConstraint != null ? nativeConstraint.getCurrentSpeed() : 0.0; }
        }
        public double currentForce
        {
            get { return (lockController != null ? lockController.getCurrentForce() : 0.0) +
                         (targetSpeedController != null ? targetSpeedController.getCurrentForce() : 0.0); }
        }

        /// <summary>
        /// 実際のAGXUnityのコンストレイントを制御方法によって準備する。
        /// </summary>
        public void Initialize()
        {
            if (constraint?.GetInitialized<Constraint>() != null)
            {
                nativeConstraint = agx.Constraint1DOF.safeCast(constraint.Native);

                lockController = agx.LockController.safeCast(
                    constraint.GetController<LockController>()?.Native);

                targetSpeedController = agx.TargetSpeedController.safeCast(
                    constraint.GetController<TargetSpeedController>()?.Native);

                if (controlEnabled)
                {
                    UpdateControlType();
                    UpdateMaxForce();
                    UpdateControlValue();
                }
            }
        }

        /// <summary>
        /// controlValueを実際のAGXUnityのコンストレイントに設定する。
        /// </summary>
        public void UpdateConstraintControl()
        {
            if (!controlEnabled)
                return;

            if (controlType != controlTypePrev)
                UpdateControlType();

            if (controlMaxForce != controlMaxForcePrev)
                UpdateMaxForce();

            if (controlValue != controlValuePrev)
                UpdateControlValue();
        }

        #endregion

        #region Private

        ControlType? controlTypePrev = null;
        double? controlValuePrev = null;　// controlValueが変わったか検知するための値。
        double? controlMaxForcePrev = null;

        agx.Constraint1DOF nativeConstraint;
        agx.LockController lockController;
        agx.TargetSpeedController targetSpeedController;
        agx.ElementaryConstraint activeController;

        /// <summary>
        /// controlTypeによって、lockControllerかtargetSpeedControllerをEnable
        /// </summary>
        void UpdateControlType()
        {
            activeController = controlType == ControlType.Position ?
                (agx.ElementaryConstraint) lockController : targetSpeedController;

            if (lockController != null)
                lockController.setEnable(activeController == lockController);

            if (targetSpeedController != null)
                targetSpeedController.setEnable(activeController == targetSpeedController);

            controlTypePrev = controlType;
            controlValuePrev = null;
            controlMaxForcePrev = null;
        }

        void UpdateMaxForce()
        {
            if(activeController != null && controlType != ControlType.Force)
                activeController.setForceRange(new agx.RangeReal(controlMaxForce));

            controlMaxForcePrev = controlMaxForce;
        }

        void UpdateControlValue()
        {
            switch (controlType)
            {
                case ControlType.Position:
                    if (lockController != null)
                        lockController.setPosition(controlValue);
                    break;
                case ControlType.Speed:
                    if (targetSpeedController != null)
                        targetSpeedController.setSpeed(controlValue);
                    break;
                case ControlType.Force:
                    if (targetSpeedController != null)
                    {
                        double dir = controlValue > 0.0 ? 1.0 : (controlValue < 0.0 ? -1.0 : 0.0);
                        targetSpeedController.setSpeed(dir * float.PositiveInfinity);
                        targetSpeedController.setForceRange(controlValue, controlValue);
                    }
                    break;
            }
            controlValuePrev = controlValue;
        }

        #endregion
    }
}