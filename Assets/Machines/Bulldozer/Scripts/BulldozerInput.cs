using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PWRISimulator.ROS
{
    public class BulldozerInput : MonoBehaviour
    {
        public BulldozerBladeSubscriber BladeSubscriber;
        public TrackMessageSubscriber trackSubscriber;
        public BulldozerSettingSubscriber settingSubscriber;
        [SerializeField] ConstractionMovementControlType movementControlType;
        [SerializeField] ControlType controlType = ControlType.Position;

        [Header("Blade lift limit (blade edge height)")]
        [SerializeField] Transform bladeEdge;
        [SerializeField] float bladeEdgeHeightUpperLimitMeters = 0.8f;
        [SerializeField] float bladeEdgeHeightLowerLimitMeters = -0.38f;
        [SerializeField] float liftPositionRateLimitRadPerSec = 0.5f;

        [Header("Blade tilt limit (blade edge end height difference)")]
        [SerializeField] Transform bladeEdgeLeft;
        [SerializeField] Transform bladeEdgeRight;
        [SerializeField] float bladeEdgeEndHeightDifferenceLimitMeters = 0.435f;
        [SerializeField] float tiltPositionRateLimitRadPerSec = 0.5f;

        [Header("Blade angle limit")]
        [SerializeField] float bladeAngleLimitDegrees = 24.0f;

        [Header("Blade edge debug")]
        [PWRISimulator.ReadOnly] [SerializeField] float currentBladeHeightMeters;
        [PWRISimulator.ReadOnly] [SerializeField] float currentBladeEdgeDifferenceMeters;

        [Header("Length convertor")]
        public BladeLiftToCylinderLengthConvertor bladeLiftCylConv;
        public BladeTiltToCylinderLengthConvertor bladeTiltCylConv;
        public BladeAngleToCylinderLengthConvertor bladeAngleCylConv;

        public TrackTwistCommandConvertor twistCommandConvertor;

        [Header("Dummy")]
        [SerializeField] bool enabledDummy;
        [SerializeField] double lift_joint;
        [SerializeField] double tilt_joint;
        [SerializeField] double angle_joint;
        [SerializeField] double right_track;
        [SerializeField] double left_track;
        [SerializeField] bool emergencyStop;


        private BulldozerJoints joints;
        private const string BladeEdgeGameObjectName = "AGXUnity.Collide.Box_blade3";
        private const string BladeEdgeLeftAutoName = "BladeEdgeLeft_Auto";
        private const string BladeEdgeRightAutoName = "BladeEdgeRight_Auto";
        private float bladeEdgeInitialLocalY;
        private float bladeEdgeLeftInitialLocalY;
        private float bladeEdgeRightInitialLocalY;
        private bool bladeEdgeHeightInitialized;
        private bool bladeEdgeEndsInitialized;

        void Start()
        {
            joints = gameObject.GetComponent<BulldozerJoints>();

            if (!TryInitializeBladeEdgeReferences())
            {
                Debug.LogWarning($"[{nameof(BulldozerInput)}] Failed to find blade edge GameObject '{BladeEdgeGameObjectName}' at Start(). Height-based lift limiting will be disabled.", this);
            }
        }

        private bool TryInitializeBladeEdgeReferences()
        {
            if (bladeEdge == null)
            {
                var bladeEdgeGo = GameObject.Find(BladeEdgeGameObjectName);
                bladeEdge = bladeEdgeGo != null ? bladeEdgeGo.transform : null;
            }

            if (bladeEdge != null && !bladeEdgeHeightInitialized)
            {
                bladeEdgeInitialLocalY = transform.InverseTransformPoint(bladeEdge.position).y;
                bladeEdgeHeightInitialized = true;
            }

            if ((bladeEdgeLeft == null || bladeEdgeRight == null) && bladeEdge != null)
            {
                if (TryCreateBladeEdgeEndReferencesFromBounds(bladeEdge, out Transform left, out Transform right))
                {
                    if (bladeEdgeLeft == null)
                        bladeEdgeLeft = left;
                    if (bladeEdgeRight == null)
                        bladeEdgeRight = right;
                }
            }

            if (bladeEdgeLeft != null && bladeEdgeRight != null && !bladeEdgeEndsInitialized)
            {
                bladeEdgeLeftInitialLocalY = transform.InverseTransformPoint(bladeEdgeLeft.position).y;
                bladeEdgeRightInitialLocalY = transform.InverseTransformPoint(bladeEdgeRight.position).y;
                bladeEdgeEndsInitialized = true;
            }

            return bladeEdgeHeightInitialized || bladeEdgeEndsInitialized;
        }

        private bool TryCreateBladeEdgeEndReferencesFromBounds(Transform bladeEdgeTransform, out Transform left, out Transform right)
        {
            left = null;
            right = null;

            if (!TryGetBladeEdgeBounds(bladeEdgeTransform, out Bounds bounds))
                return false;

            Vector3[] corners = new Vector3[8];
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
            corners[1] = c + new Vector3(-e.x, -e.y,  e.z);
            corners[2] = c + new Vector3(-e.x,  e.y, -e.z);
            corners[3] = c + new Vector3(-e.x,  e.y,  e.z);
            corners[4] = c + new Vector3( e.x, -e.y, -e.z);
            corners[5] = c + new Vector3( e.x, -e.y,  e.z);
            corners[6] = c + new Vector3( e.x,  e.y, -e.z);
            corners[7] = c + new Vector3( e.x,  e.y,  e.z);

            int minIdx = 0;
            int maxIdx = 0;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                float localX = transform.InverseTransformPoint(corners[i]).x;
                if (localX < minX)
                {
                    minX = localX;
                    minIdx = i;
                }
                if (localX > maxX)
                {
                    maxX = localX;
                    maxIdx = i;
                }
            }

            Transform leftExisting = bladeEdgeTransform.Find(BladeEdgeLeftAutoName);
            Transform rightExisting = bladeEdgeTransform.Find(BladeEdgeRightAutoName);

            if (leftExisting == null)
                leftExisting = new GameObject(BladeEdgeLeftAutoName).transform;
            if (rightExisting == null)
                rightExisting = new GameObject(BladeEdgeRightAutoName).transform;

            leftExisting.SetParent(bladeEdgeTransform, true);
            rightExisting.SetParent(bladeEdgeTransform, true);

            leftExisting.position = corners[minIdx];
            rightExisting.position = corners[maxIdx];

            left = leftExisting;
            right = rightExisting;
            return true;
        }

        private bool TryGetBladeEdgeBounds(Transform bladeEdgeTransform, out Bounds bounds)
        {
            bounds = default;
            if (bladeEdgeTransform == null)
                return false;

            if (bladeEdgeTransform.TryGetComponent<AGXUnity.Collide.Box>(out var agxBox))
            {
                Vector3 halfExtents = agxBox.HalfExtents;
                Vector3[] corners = new Vector3[8];
                int index = 0;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            corners[index++] = bladeEdgeTransform.TransformPoint(new Vector3(
                                halfExtents.x * x,
                                halfExtents.y * y,
                                halfExtents.z * z));
                        }
                    }
                }

                bounds = new Bounds(corners[0], Vector3.zero);
                for (int i = 1; i < corners.Length; i++)
                    bounds.Encapsulate(corners[i]);
                return true;
            }

            if (bladeEdgeTransform.TryGetComponent<BoxCollider>(out var boxCollider))
            {
                bounds = boxCollider.bounds;
                return true;
            }

            if (bladeEdgeTransform.TryGetComponent<Collider>(out var collider))
            {
                bounds = collider.bounds;
                return true;
            }

            if (bladeEdgeTransform.TryGetComponent<Renderer>(out var renderer))
            {
                bounds = renderer.bounds;
                return true;
            }

            return false;
        }

        private bool TryGetBladeEdgeHeightAboveGround(out float heightMeters)
        {
            heightMeters = 0.0f;
            TryInitializeBladeEdgeReferences();
            if (bladeEdge == null || !bladeEdgeHeightInitialized)
                return false;

            float bladeEdgeLocalY = transform.InverseTransformPoint(bladeEdge.position).y;
            heightMeters = bladeEdgeLocalY - bladeEdgeInitialLocalY;
            return true;
        }

        private bool TryGetBladeEdgeEndHeightDifference(out float heightDifferenceMeters)
        {
            heightDifferenceMeters = 0.0f;
            TryInitializeBladeEdgeReferences();
            if (bladeEdgeLeft == null || bladeEdgeRight == null || !bladeEdgeEndsInitialized)
                return false;

            float leftLocalY = transform.InverseTransformPoint(bladeEdgeLeft.position).y - bladeEdgeLeftInitialLocalY;
            float rightLocalY = transform.InverseTransformPoint(bladeEdgeRight.position).y - bladeEdgeRightInitialLocalY;
            heightDifferenceMeters = leftLocalY - rightLocalY;
            return true;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            UpdateBladeEdgeDebugValues();

            if (enabledDummy)
            {
                BladeSubscriber.BladeCmd.position[0] = lift_joint;
                BladeSubscriber.BladeCmd.position[1] = tilt_joint;
                BladeSubscriber.BladeCmd.position[2] = angle_joint;
                trackSubscriber.TrackCmd.position[0] = right_track;
                trackSubscriber.TrackCmd.position[1] = left_track;
            }
            else
            {
                // 受信
                double currentTime = Time.fixedTimeAsDouble - Time.fixedDeltaTime;

                BladeSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
                trackSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
                settingSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
            }
            // 制御値の反映
            if (settingSubscriber.EmergencyStopCmd)
            {
                // 緊急停止
            }
            else
            {
                // 上部旋回体
                switch(controlType)
                {
                    case ControlType.Position:
                        break;
                    case ControlType.Speed:
                        break;
                    case ControlType.Force:
                        break;
                    default:
                        break;
                }
                // 下部走行体
                switch(movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        switch(controlType)
                        {
                            case ControlType.Position:
                                break;
                            case ControlType.Speed:
                                break;
                            case ControlType.Force:
                                break;
                            default:
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        break;
                    case ConstractionMovementControlType.VolumeCommand:
                        break;
                    default:
                        break;
                }
            }
        }

        private void UpdateBladeEdgeDebugValues()
        {
            currentBladeHeightMeters = TryGetBladeEdgeHeightAboveGround(out float bladeHeight) ? bladeHeight : 0.0f;
            currentBladeEdgeDifferenceMeters = TryGetBladeEdgeEndHeightDifference(out float edgeDifference) ? edgeDifference : 0.0f;
        }

        private float BladeAngleLimitRadians => Mathf.Abs(bladeAngleLimitDegrees) * Mathf.Deg2Rad;

        private float ClampBladeAngle(float angleRadians)
        {
            return Mathf.Clamp(angleRadians, -BladeAngleLimitRadians, BladeAngleLimitRadians);
        }

        public void SetCommands()
        {
            // 制御値の反映
            if (enabledDummy ? emergencyStop : settingSubscriber.EmergencyStopCmd)
            {
                // 緊急停止
                joints.bladeLift.controlType = ControlType.Position;
                joints.bladeLift.controlValue = joints.bladeLift.CurrentPosition;

                joints.bladeTilt.controlType = ControlType.Position;
                joints.bladeTilt.controlValue = joints.bladeTilt.CurrentPosition;

                joints.bladeAngleLeft.controlType = ControlType.Position;
                joints.bladeAngleLeft.controlValue = joints.bladeAngleLeft.CurrentPosition;

                joints.bladeAngleRight.controlType = ControlType.Position;
                joints.bladeAngleRight.controlValue = joints.bladeAngleRight.CurrentPosition;

                joints.rightSprocket.controlType = ControlType.Position;
                joints.rightSprocket.controlValue = joints.rightSprocket.CurrentPosition;

                joints.leftSprocket.controlType = ControlType.Position;
                joints.leftSprocket.controlValue = joints.leftSprocket.CurrentPosition;
            }
            else
            {
                // 上部旋回体
                switch (controlType)
                {
                    case ControlType.Position:
                        float targetLiftCmdAngle = (float)BladeSubscriber.BladeCmd.position[0];
                        float maxDelta = liftPositionRateLimitRadPerSec > 0.0f ? liftPositionRateLimitRadPerSec * Time.fixedDeltaTime : float.PositiveInfinity;
                        float currentLiftAngle = bladeLiftCylConv.currentLinkAngle;
                        float liftCmdAngle = Mathf.MoveTowards(currentLiftAngle, targetLiftCmdAngle, maxDelta);
                        double liftControlValue = bladeLiftCylConv.CalculateCylinderRodTelescoping(liftCmdAngle);

                        if (TryGetBladeEdgeHeightAboveGround(out float bladeEdgeHeight) && joints != null)
                        {
                            double currentLiftControlValue = joints.bladeLift.CurrentPosition;
                            if (bladeEdgeHeight >= bladeEdgeHeightUpperLimitMeters &&
                                liftControlValue < currentLiftControlValue)
                            {
                                liftControlValue = currentLiftControlValue;
                            }
                            else if (bladeEdgeHeight <= bladeEdgeHeightLowerLimitMeters &&
                                     liftControlValue > currentLiftControlValue)
                            {
                                liftControlValue = currentLiftControlValue;
                            }
                        }

                        joints.bladeLift.controlType = ControlType.Position;
                        joints.bladeLift.controlValue = liftControlValue;

                        float targetTiltCmdAngle = (float)BladeSubscriber.BladeCmd.position[1];
                        float maxTiltDelta = tiltPositionRateLimitRadPerSec > 0.0f ? tiltPositionRateLimitRadPerSec * Time.fixedDeltaTime : float.PositiveInfinity;
                        float currentTiltAngle = bladeTiltCylConv.currentLinkAngle;
                        float tiltCmdAngle = targetTiltCmdAngle; // Mathf.MoveTowards(currentTiltAngle, targetTiltCmdAngle, maxTiltDelta);
                        double tiltControlValue = bladeTiltCylConv.CalculateCylinderRodTelescoping(tiltCmdAngle);

                        if (TryGetBladeEdgeEndHeightDifference(out float endHeightDiff) && joints != null)
                        {
                            double currentTiltControlValue = joints.bladeTilt.CurrentPosition;
                            if (endHeightDiff >= bladeEdgeEndHeightDifferenceLimitMeters &&
                                tiltControlValue < currentTiltControlValue)
                            {
                                tiltControlValue = currentTiltControlValue;
                            }
                            if (endHeightDiff <= - bladeEdgeEndHeightDifferenceLimitMeters &&
                                tiltControlValue > currentTiltControlValue)
                            {
                                tiltControlValue = currentTiltControlValue;
                            }
                        }

                        joints.bladeTilt.controlType = ControlType.Position;
                        joints.bladeTilt.controlValue = tiltControlValue;

                        float clampedBladeAngle = ClampBladeAngle((float)BladeSubscriber.BladeCmd.position[2]);
                        float telescoping = bladeAngleCylConv.CalculateCylinderRodTelescoping(clampedBladeAngle);
                        joints.bladeAngleLeft.controlType = ControlType.Position;
                        joints.bladeAngleLeft.controlValue = -telescoping;

                        joints.bladeAngleRight.controlType = ControlType.Position;
                        joints.bladeAngleRight.controlValue = telescoping;
                        break;
                    case ControlType.Speed:
                        float liftCmdVelocity = (float)BladeSubscriber.BladeCmd.velocity[0];
                        double liftControlVelocity = bladeLiftCylConv.CalculateCylinderRodTelescopingVelocity(liftCmdVelocity);
                        if (TryGetBladeEdgeHeightAboveGround(out float bladeEdgeHeightVel))
                        {
                            if (bladeEdgeHeightVel >= bladeEdgeHeightUpperLimitMeters && liftControlVelocity > 0.0)
                                liftControlVelocity = 0.0;
                            else if (bladeEdgeHeightVel <= bladeEdgeHeightLowerLimitMeters && liftControlVelocity < 0.0)
                                liftControlVelocity = 0.0;
                        }

                        joints.bladeLift.controlType = ControlType.Speed;
                        joints.bladeLift.controlValue = liftControlVelocity;

                        float tiltCmdVelocity = (float)BladeSubscriber.BladeCmd.velocity[1];
                        double tiltControlVelocity = bladeTiltCylConv.CalculateCylinderRodTelescopingVelocity(tiltCmdVelocity);
                        if (TryGetBladeEdgeEndHeightDifference(out float endHeightDiffVel) && joints != null)
                        {
                            if (endHeightDiffVel >= bladeEdgeEndHeightDifferenceLimitMeters && tiltControlVelocity > 0.0)
                                tiltControlVelocity = 0.0;
                            if (endHeightDiffVel <= -bladeEdgeEndHeightDifferenceLimitMeters && tiltControlVelocity < 0.0)
                                tiltControlVelocity = 0.0;
                        }

                        joints.bladeTilt.controlType = ControlType.Speed;
                        joints.bladeTilt.controlValue = tiltControlVelocity;

                        float bladeAngleVelocity = (float)BladeSubscriber.BladeCmd.velocity[2];
                        float currentBladeAngle = bladeAngleCylConv.currentLinkAngle;
                        float clampedBladeAngleVelocity = bladeAngleVelocity;
                        if ((currentBladeAngle >= BladeAngleLimitRadians && bladeAngleVelocity > 0.0f) ||
                            (currentBladeAngle <= -BladeAngleLimitRadians && bladeAngleVelocity < 0.0f))
                        {
                            clampedBladeAngleVelocity = 0.0f;
                        }

                        float telescopingVelocity = bladeAngleCylConv.CalculateCylinderRodTelescopingVelocity(clampedBladeAngleVelocity);
                        joints.bladeAngleLeft.controlType = ControlType.Speed;
                        joints.bladeAngleLeft.controlValue = -telescopingVelocity;

                        joints.bladeAngleRight.controlType = ControlType.Speed;
                        joints.bladeAngleRight.controlValue = telescopingVelocity;
                        break;
                    case ControlType.Force:
                        float liftCmdForce = (float)BladeSubscriber.BladeCmd.effort[0];
                        double liftControlForce = bladeLiftCylConv.CalculateCylinderRodTelescopingForce(liftCmdForce);
                        if (TryGetBladeEdgeHeightAboveGround(out float bladeEdgeHeightForce))
                        {
                            if (bladeEdgeHeightForce >= bladeEdgeHeightUpperLimitMeters && liftControlForce > 0.0)
                                liftControlForce = 0.0;
                            else if (bladeEdgeHeightForce <= bladeEdgeHeightLowerLimitMeters && liftControlForce < 0.0)
                                liftControlForce = 0.0;
                        }

                        joints.bladeLift.controlType = ControlType.Force;
                        joints.bladeLift.controlValue = liftControlForce;

                        double tiltControlForce = bladeTiltCylConv.CalculateCylinderRodTelescopingForce((float)BladeSubscriber.BladeCmd.effort[1]);
                        if (TryGetBladeEdgeEndHeightDifference(out float endHeightDiffForce) && joints != null)
                        {
                            if (endHeightDiffForce >= bladeEdgeEndHeightDifferenceLimitMeters && tiltControlForce > 0.0)
                                tiltControlForce = 0.0;
                            if (endHeightDiffForce <= -bladeEdgeEndHeightDifferenceLimitMeters && tiltControlForce < 0.0)
                                tiltControlForce = 0.0;
                        }

                        joints.bladeTilt.controlType = ControlType.Force;
                        joints.bladeTilt.controlValue = tiltControlForce;

                        float bladeAngleForce = (float)BladeSubscriber.BladeCmd.effort[2];
                        float currentBladeAngleForce = bladeAngleCylConv.currentLinkAngle;
                        float clampedBladeAngleForce = bladeAngleForce;
                        if ((currentBladeAngleForce >= BladeAngleLimitRadians && bladeAngleForce > 0.0f) ||
                            (currentBladeAngleForce <= -BladeAngleLimitRadians && bladeAngleForce < 0.0f))
                        {
                            clampedBladeAngleForce = 0.0f;
                        }

                        float telescopingForce = bladeAngleCylConv.CalculateCylinderRodTelescopingForce(clampedBladeAngleForce);
                        joints.bladeAngleLeft.controlType = ControlType.Force;
                        joints.bladeAngleLeft.controlValue = -telescopingForce;

                        joints.bladeAngleRight.controlType = ControlType.Force;
                        joints.bladeAngleRight.controlValue = telescopingForce;
                        break;
                    default:
                        break;
                }
                // 下部走行体
                switch (movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        switch (controlType)
                        {
                            case ControlType.Position:
                                joints.rightSprocket.controlType = ControlType.Position;
                                joints.rightSprocket.controlValue = trackSubscriber.TrackCmd.position[0];

                                joints.leftSprocket.controlType = ControlType.Position;
                                joints.leftSprocket.controlValue = trackSubscriber.TrackCmd.position[1];
                                break;
                            case ControlType.Speed:
                                joints.rightSprocket.controlType = ControlType.Speed;
                                joints.rightSprocket.controlValue = trackSubscriber.TrackCmd.velocity[0];

                                joints.leftSprocket.controlType = ControlType.Speed;
                                joints.leftSprocket.controlValue = trackSubscriber.TrackCmd.velocity[1];
                                break;
                            case ControlType.Force:
                                joints.rightSprocket.controlType = ControlType.Force;
                                joints.rightSprocket.controlValue = trackSubscriber.TrackCmd.effort[0];

                                joints.leftSprocket.controlType = ControlType.Force;
                                joints.leftSprocket.controlValue = trackSubscriber.TrackCmd.effort[1];
                                break;
                            default:
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        twistCommandConvertor.SetCommand(trackSubscriber.VelocityCmd.linear, trackSubscriber.VelocityCmd.angular);

                        joints.leftSprocket.controlType = ControlType.Speed;
                        joints.leftSprocket.controlValue = twistCommandConvertor.sprocketSpeed_L;

                        joints.rightSprocket.controlType = ControlType.Speed;
                        joints.rightSprocket.controlValue = twistCommandConvertor.sprocketSpeed_R;
                        break;
                    case ConstractionMovementControlType.VolumeCommand:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
