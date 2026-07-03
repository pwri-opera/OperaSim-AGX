using NUnit.Framework;
using UnityEngine;

namespace PWRISimulator.Tests
{
    public class DumpVesselStateControllerTests
    {
        private GameObject _gameObject;
        private DumpVesselStateController _controller;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestVesselStateController");
            _controller = _gameObject.AddComponent<DumpVesselStateController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
        }

        /// <summary>
        /// 基本動作: 目標角度が現在角度より大きい場合、DumpUp状態になり
        /// w_up (0.5 rad/s) を返すこと。
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_TargetAboveCurrent_ReturnsDumpUpSpeed()
        {
            double w = _controller.computeAngularVelocity(0.0, 1.0);

            Assert.That(w, Is.EqualTo(_controller.w_up));
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpUp));
        }

        /// <summary>
        /// 基本動作: 目標角度が現在角度より小さい場合、DumpDown状態になり
        /// w_down (-0.5 rad/s) を返すこと。
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_TargetBelowCurrent_ReturnsDumpDownSpeed()
        {
            double w = _controller.computeAngularVelocity(1.0, 0.0);

            Assert.That(w, Is.EqualTo(_controller.w_down));
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpDown));
        }

        /// <summary>
        /// 目標に到達した場合、Stop状態になり 0.0 を返すこと。
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_AtTarget_ReturnsZeroAndStop()
        {
            _controller.computeAngularVelocity(0.0, 1.0); // DumpUpへ
            double w = _controller.computeAngularVelocity(1.0, 1.0); // 目標に到達

            Assert.That(w, Is.EqualTo(0.0));
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.Stop));
        }

        /// <summary>
        /// issue #79 の再現: DumpUp中にオーバーシュートが発生するとStop状態に遷移する。
        /// その後、同じ目標値を再送しても従来はStopから復帰できなかった（targetChanged=falseのため）。
        /// 修正後は、目標に到達していない場合は同じ目標値でも復帰できること。
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_OvershootThenSameTarget_RestartsFromStop_Issue79()
        {
            // 1. 目標 1.0 に向けて上昇開始
            _controller.computeAngularVelocity(0.0, 1.0);
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpUp));

            // 2. オーバーシュート: 現在角度が目標を超えた (1.25 > 1.0)
            double w = _controller.computeAngularVelocity(1.25, 1.0);

            // DumpUp状態で pt < pc のため Stop に遷移
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.Stop));
            Assert.That(w, Is.EqualTo(0.0));

            // 3. 同じ目標値 1.0 を再送（targetChanged = false）
            // 修正前: Stopのまま → 0.0 を返す（バグ）
            // 修正後: 目標に到達していないため DumpDown に復帰 → w_down を返す
            double w2 = _controller.computeAngularVelocity(1.25, 1.0);

            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpDown),
                "オーバーシュート後に同じ目標値を再送した場合、Stopから復帰して" +
                "DumpDown状態になるべき（issue #79: targetChangedのみに依存しない）");
            Assert.That(w2, Is.EqualTo(_controller.w_down));
        }

        /// <summary>
        /// オーバーシュート後、異なる目標値を送った場合はDumpDownに遷移すること。
        /// （従来動作の回帰テスト）
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_OvershootThenDifferentTarget_TransitionsToDumpDown()
        {
            // 上昇 → オーバーシュート
            _controller.computeAngularVelocity(0.0, 1.0);
            _controller.computeAngularVelocity(1.25, 1.0);
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.Stop));

            // 新しい目標値 0.5 を送信
            double w = _controller.computeAngularVelocity(1.25, 0.5);

            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpDown));
            Assert.That(w, Is.EqualTo(_controller.w_down));
        }

        /// <summary>
        /// DumpDown中のオーバーシュート（現在角度が目標を下回る）でも
        /// 同じ目標値で復帰できること。
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_DownOvershootThenSameTarget_RestartsFromStop_Issue79()
        {
            // 1. 目標 0.0 に向けて下降開始
            _controller.computeAngularVelocity(1.0, 0.0);
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpDown));

            // 2. オーバーシュート: 現在角度が目標を下回った (-0.25 < 0.0)
            double w = _controller.computeAngularVelocity(-0.25, 0.0);

            // DumpDown状態で pt > pc のため Stop に遷移
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.Stop));
            Assert.That(w, Is.EqualTo(0.0));

            // 3. 同じ目標値 0.0 を再送
            double w2 = _controller.computeAngularVelocity(-0.25, 0.0);

            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.DumpUp),
                "下降オーバーシュート後に同じ目標値を再送した場合、Stopから復帰して" +
                "DumpUp状態になるべき（issue #79）");
            Assert.That(w2, Is.EqualTo(_controller.w_up));
        }

        /// <summary>
        /// 目標に到達後（Stop状態）、同じ目標値を送った場合は0.0を返すこと。
        /// （早期リターンによる正しい動作の確認）
        /// </summary>
        [Test]
        public void ComputeAngularVelocity_AtTargetSameCommand_ReturnsZero()
        {
            // 目標に到達
            _controller.computeAngularVelocity(0.0, 1.0);
            _controller.computeAngularVelocity(1.0, 1.0);
            Assert.That(_controller.CurrentState, Is.EqualTo(DumpState.Stop));

            // 同じ目標値を再送
            double w = _controller.computeAngularVelocity(1.0, 1.0);

            Assert.That(w, Is.EqualTo(0.0),
                "目標に到達している場合は同じ目標値を送っても0.0を返すべき");
        }
    }
}
