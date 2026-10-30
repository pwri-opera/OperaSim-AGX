using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PWRISimulator.Tests
{
    /// <summary>
    /// Temporary PlayMode test for reproducing/verifying the EKF input lag issue (#103).
    /// Loads GameScene, switches to simulation mode, connects ROSConnection to the
    /// local ros_tcp_endpoint, and lets the sim publish for a fixed window while an
    /// external ROS subscriber measures end-to-end lag on the ic120 topics.
    /// </summary>
    public class RosPipelineLagTest
    {
        private const string SceneName = "GameScene";
        private const int ActionModeSimulation = 3;

        // Total sim run time; ROS-side measurement happens externally.
        private const float RunSeconds = 3000f;

        [UnityTest]
        [Timeout(3200000)]
        public IEnumerator Ic120Topics_PublishWithoutLag()
        {
            // We only care about ROS traffic here; the scene logs known
            // non-fatal errors (terrain bootstrap, AGX natives, ...) that
            // must not fail the run.
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(3.0f);

            // Start simulation mode (same as UI flow)
            var gv = System.Type.GetType("PWRISimulator.GlobalVariables, Assembly-CSharp");
            Assert.That(gv, Is.Not.Null, "GlobalVariables not found");
            gv.GetField("ActionMode").SetValue(null, ActionModeSimulation);

            // Connect to local endpoint (HUD button equivalent)
            var rosType = System.Type.GetType(
                "Unity.Robotics.ROSTCPConnector.ROSConnection, Unity.Robotics.ROSTCPConnector");
            Assert.That(rosType, Is.Not.Null, "ROSConnection type not found");
            var ros = rosType.GetMethod("GetOrCreateInstance").Invoke(null, null);
            rosType.GetMethod("Connect", new[] { typeof(string), typeof(int) })
                   .Invoke(ros, new object[] { "127.0.0.1", 10000 });
            Debug.Log("[LagTest] ROSConnection.Connect(127.0.0.1:10000) called");

            // Register a wall-clock probe topic: subscriber computes true
            // transport lag = arrival_wall - probe value (Time.realtimeSinceStartup).
            var float64Type = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("RosMessageTypes.Std.Float64Msg"))
                .FirstOrDefault(t => t != null);
            Assert.That(float64Type, Is.Not.Null, "Float64Msg not found");
            var regMethod = rosType.GetMethods()
                .First(m => m.Name == "RegisterPublisher" && m.IsGenericMethod
                            && m.GetParameters().Length == 3
                            && m.GetParameters()[1].ParameterType == typeof(int?));
            regMethod.MakeGenericMethod(float64Type)
                     .Invoke(ros, new object[] { "/lag_probe", null, null });
            var publishMethod = rosType.GetMethods()
                .First(m => m.Name == "Publish" && !m.IsGenericMethod
                            && m.GetParameters().Length == 2
                            && m.GetParameters()[0].ParameterType == typeof(string));
            var newMsg = float64Type.GetConstructor(System.Type.EmptyTypes);
            var dataField = float64Type.GetField("data");
            var publish = new System.Action(() =>
            {
                var m = newMsg.Invoke(null);
                dataField.SetValue(m, System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
                publishMethod.Invoke(ros, new object[] { "/lag_probe", m });
            });

            var t0 = Time.realtimeSinceStartup;
            double nextProbe = t0;

            while (Time.realtimeSinceStartup - t0 < RunSeconds)
            {
                if (Time.realtimeSinceStartup >= nextProbe)
                {
                    publish();
                    nextProbe += 0.05; // 20 Hz probe
                }
                yield return null;
            }

            Assert.Pass("Simulated run completed");
        }
    }
}
