using System;
using UnityEngine;

public enum DumpState { DumpUp, DumpDown, Stop }

/// <summary>
/// ダンプのベッセル状態を管理するためのクラス
/// pt: 目標角度, prevPt: 1ステップ前の目標角度, pc: 現在角度, eps: 閾値
/// </summary> 
namespace PWRISimulator
{
    public class DumpVesselStateController : MonoBehaviour
    {
        [Header("Constant angular velocities")]
        [Tooltip("DumpUp 時の入力角速度（>0 を推奨）")]
        public double w_up = 0.5;

        [Tooltip("DumpDown 時の入力角速度（<0 を推奨）")]
        public double w_down = -0.5;

        private double _eps = 0.005 * Mathf.Deg2Rad;  // 0.5 [deg]

        // インスタンス内で保持（外部からはいじらせない）
        DumpState _state = DumpState.Stop;
        double _prevTarget;
        bool _hasPrev = false;   // 初回だけ prev が未定義



        public DumpState CurrentState => _state;


        /// <summary>
        /// 現在角(currentAngle), 目標角(targetAngle), 許容誤差(_eps) から出力角速度 w を計算して返す（内部で状態更新も行う）
        /// pt:     目標角度
        /// prevPt: 1ステップ前の目標角度,
        /// pc:     現在角度
        /// _eps:    閾値
        /// </summary>
        public double computeAngularVelocity(double currentAngle, double targetAngle)
        {
            double pc = currentAngle;
            double pt = targetAngle;

            // Stop の自己遷移条件： |pt - pc| < _eps
            if (Math.Abs(pt - pc) < _eps)
            {
                _state = DumpState.Stop;
                _prevTarget = pt;
                _hasPrev = true;
                return 0.0;
            }

            // 「t が更新判定
            bool targetChanged = !_hasPrev || pt != _prevTarget;

            switch (_state)
            {
                case DumpState.DumpUp:
                    if (pt >= pc) { /* keep DumpUp */ } 
                    if (pt < pc) _state = DumpState.Stop;
                    if (pt < pc && targetChanged) _state = DumpState.DumpDown;
                    break;

                case DumpState.DumpDown:
                    if (pt <= pc) { /* keep DumpDown */ }
                    if (pt > pc) _state = DumpState.Stop;
                    if (pt > pc && targetChanged) _state = DumpState.DumpUp;
                    break;

                case DumpState.Stop:
                default:
                    // 再始動は「目標が更新された時のみ」
                    if (targetChanged)
                    {
                        if (pt > pc) _state = DumpState.DumpUp;
                        else if (pt < pc) _state = DumpState.DumpDown;
                    }
                    break;
            }

            _prevTarget = pt;
            _hasPrev = true;

            // 状態に応じた角速度を返す
            switch (_state)
            {
                case DumpState.DumpUp:   return w_up;
                case DumpState.DumpDown: return w_down;
                case DumpState.Stop:
                default:                 return 0.0;
            }
        }
    }
}