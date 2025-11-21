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

        /// <summary>
        /// むだ時間。入力が与えられた際の Constraintが制御により動き出すための時間（ms）
        /// </summary>
        [ConditionalHide("controlEnabled", true)]
        public double deadTime = 0.0;


        // [Header("Constraint State")]
        // [ConditionalHide("controlEnabled", true)][SerializeField] public bool isEnableState = false;
        // [ConditionalHide("controlEnabled", true)][SerializeField] public double forceRangeMaxState;
        // [ConditionalHide("controlEnabled", true)][SerializeField] public double forceRangeMinState;
        // [ConditionalHide("controlEnabled", true)][SerializeField] public double forceState;
        // [ConditionalHide("controlEnabled", true)][SerializeField] public double compState;
        // [ConditionalHide("controlEnabled", true)][SerializeField] public double dampState;

        // [ConditionalHide("controlEnabled", true)][SerializeField] public double reguComp;
        // [ConditionalHide("controlEnabled", true)][SerializeField] public double reguDump;

        public double CurrentPosition
        {
            get { return nativeConstraint != null ? nativeConstraint.getAngle() : 0.0; }
        }
        public double CurrentSpeed
        {
            get { return nativeConstraint != null ? nativeConstraint.getCurrentSpeed() : 0.0; }
        }
        public double CurrentForce
        {
            get { return (lockController != null ? lockController.getCurrentForce() : 0.0) +
                         (targetSpeedController != null ? targetSpeedController.getCurrentForce() : 0.0); }
        }

        /// <summary>
        /// 実際のAGXUnityのコンストレイントを制御方法によって準備する。
        /// </summary>
        public void Initialize()
        {
            // Debug.Log($"[{nameof(ConstraintControl)}] Applying Params 72...");
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

            // Constraint 内各パラメータを確認するための関数
            // GetConstraintParam();
        }

        private ControlType? controlTypePrev = null;
        private double? controlValuePrev = null;// controlValueが変わったか検知するための値。
        private double? controlMaxForcePrev = null;

        private agx.Constraint1DOF nativeConstraint;
        private agx.LockController lockController;
        private agx.TargetSpeedController targetSpeedController;
        private agx.ElementaryConstraint activeController;

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
                { 
                targetSpeedController.setEnable(activeController == targetSpeedController);
                targetSpeedController.setLockedAtZeroSpeed(activeController == targetSpeedController);
                }

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

        public (double, double) UpdateComplianceDamping(double comp, double dump)
        {
            double c = -1.0;
            double d = -1.0; 

            switch (controlType)
            {
                case ControlType.Position:
                    if (lockController != null)
                    {
                        lockController.setCompliance(comp);
                        lockController.setDamping(dump);
                        c = lockController.getCompliance();
                        d = lockController.getDamping();
                    }
                    break;
                case ControlType.Speed:
                    if (targetSpeedController != null)
                    {
                        targetSpeedController.setCompliance(comp);
                        targetSpeedController.setDamping(comp);
                        c = targetSpeedController.getCompliance();
                        d = targetSpeedController.getDamping();                        
                    }
                    break;
                case ControlType.Force:
                    break;
            }
            return (c, d);
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
        // // Constraint 内各パラメータを確認するための関数
        // void GetConstraintParam()
        // {

        //     isEnableState = activeController.getEnable();

        //     if (activeController != null) {
        //         agx.RangeReal r = activeController.getForceRange();
        //         forceRangeMaxState = r.upper();
        //         forceRangeMinState = r.lower();
        //         if (controlType == ControlType.Position || controlType == ControlType.Speed) {
        //             compState = activeController.getCompliance();
        //             dampState = activeController.getDamping();
        //             reguComp = activeController.getRegularizationParameters().getCompliance();
        //             reguDump = activeController.getRegularizationParameters().getDamping();                                      
        //         }
        //         forceState = activeController.getCurrentForce();
        //     }
        //     // isEnableState = lockController.getEnable();
        //     // AGXUnity.RangeReal r = lockController.ForceRange;
        //     // forceRangeMaxState = r.Upper;
        //     // forceRangeMinState = r.Lower;

        //     // compState = lockController.getCompliance();
        //     // dampState = lockController.getDamping();
        // }
    }
}