using PWRISimulator.ROS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// ���[�h���̃_���v�N���[���̃W���C���g��������
    /// </summary>
    public class SetupJointDump : MonoBehaviour
    {
        private DumpTruckJoint joints;
        private DumpTruckInput input;

        // �S�����슮���t���O
        private bool completedFlag = false;

        // �e���̓��슮���t���O
        //private bool leftSprocketFlag = false;
        //private bool rightSprocketFlag = false;
        private bool dump_jointFlag = false;

        // Start is called before the first frame update
        void Start()
        {
            UnityEngine.Debug.Log("SetupJointDump!!!");

            var dumpObj = this.gameObject;

            // �N���X�ǂݍ���
            input = dumpObj.GetComponent<DumpTruckInput>();
            joints = input.joints;
        }

        // Update is called once per frame
        void Update()
        {
            if (GlobalVariables.SetupJointDumpFlag && !completedFlag)
            {
                // �d�@�̃Q�[���I�u�W�F�N�g
                var dumpObj = this.gameObject;

                // JSON�`���ŕۑ������f�[�^��Ǎ�
                var json_ms = GlobalVariables.saveMachines;

                // �I�u�W�F�N�g�Ɠ����̃f�[�^�𗘗p
                int myidx = -1;
                for (int i = 1; i < json_ms.data.Length; i++)
                {
                    UnityEngine.Debug.Log("Obj_name: " + dumpObj.name + ", json: " + json_ms.data[i].name);

                    if (json_ms.data[i].name == dumpObj.name)
                    {
                        myidx = i;
                    }
                }


                UnityEngine.Debug.Log("myidx: " + myidx);

                if (myidx > 0)
                {
                    // ControlType��Speed�i�֐߂��ړ�����p���x����͂���j
                    input.trackControlType = ControlType.Speed;
                    input.vesselControlType = ControlType.Speed;
                    input.rotateControlType = ControlType.Speed;
                    input.movementControlType = ConstractionMovementControlType.ActuatorCommand;

                    joints.leftSprocket.controlType = ControlType.Speed;
                    joints.rightSprocket.controlType = ControlType.Speed;
                    joints.dump_joint.controlType = ControlType.Speed;


                    // ���e�덷
                    const double error = 0.0174533f; // �v���X�E�}�C�i�X1�x���炢���e
                    // ���쑬�x
                    const float speed = 0.523599f; // 30�x���炢�ɐݒ�


                    // �����i�[
                    float diff = 0.0f;


                    // ���슮���m�F
                    if (json_ms.data[myidx].joint.dump_joint - error < joints.dump_joint.CurrentPosition &&
                        json_ms.data[myidx].joint.dump_joint + error > joints.dump_joint.CurrentPosition)
                    {
                        // ���x�[���Œ�~������
                        joints.dump_joint.controlValue = 0.0f;
                        // �t���O�ؑ�
                        dump_jointFlag = true;
                    }


                    // �S�����슮���m�F
                    if (dump_jointFlag)
                    {
                        // ���ڐ��������
                        input.movementControlType = ConstractionMovementControlType.TwistCommand;

                        // ���슮���t���O
                        completedFlag = true;
                        GlobalVariables.SetupJointDumpCount += 1;

                        UnityEngine.Debug.Log(GlobalVariables.Dump_ObjList.Count + ", " + GlobalVariables.SetupJointDumpCount);

                        if ((int)GlobalVariables.Dump_ObjList.Count == GlobalVariables.SetupJointDumpCount)
                        {
                            GlobalVariables.SetupJointDumpCompletedFlag = true;
                        }

                        UnityEngine.Debug.Log("Completed!!: " + GlobalVariables.SetupJointDumpCompletedFlag);
                    }


                    //UnityEngine.Debug.Log("Flag: " + dump_jointFlag);


                    // ���ɒl���Z�b�g
                    if (!dump_jointFlag)
                    {
                        diff = (float)(joints.dump_joint.CurrentPosition - json_ms.data[myidx].joint.dump_joint);
                        joints.dump_joint.controlValue = setValue(diff, speed);

                        //UnityEngine.Debug.Log("CurrentPosition: " + joints.dump_joint.CurrentPosition);
                        //UnityEngine.Debug.Log("diff: " + diff);
                        //UnityEngine.Debug.Log("controlValue: " + joints.dump_joint.controlValue);
                    }

                    joints.UpdateConstraintControls();

                }
                else
                {
                    // ���ڐ��������
                    input.movementControlType = ConstractionMovementControlType.TwistCommand;

                    // ���슮���t���O
                    completedFlag = true;
                    GlobalVariables.SetupJointDumpCount += 1;
                    if ((int)GlobalVariables.Dump_ObjList.Count >= GlobalVariables.SetupJointDumpCount)
                    {
                        GlobalVariables.SetupJointDumpCompletedFlag = true;
                    }
                }
            }


            if (!GlobalVariables.SetupJointDumpFlag && completedFlag)
            {
                completedFlag = false;
                dump_jointFlag = false;
            }
        }



        float setValue(float diff, float speed)
        {

            float result = 0.0f;

            if (Math.Abs(diff) > speed)
            {
                if (diff < 0.0f)
                {
                    result = speed;
                }
                else
                {
                    result = -1.0f * speed;
                }
            }
            else
            {
                if (diff > 0.0)
                {
                    result = -1.0f * speed * 0.5f;
                }
                else
                {
                    result = speed * 0.5f;
                }
            }
            return result;
        }
        
    }
}

