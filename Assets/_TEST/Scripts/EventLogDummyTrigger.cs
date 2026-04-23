using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// Fires dummy <see cref="GlobalVariables.ScoreEvent"/>s on keyboard
    /// input so the EventLog HUD can be verified without running a full
    /// simulation. Attach this to any scene GameObject during development
    /// and remove (or disable) the component before shipping.
    ///
    /// Key bindings:
    ///   1 - P01 Mining (+3)
    ///   2 - P02 Soil loading (+5)
    ///   3 - P03 Soil unloading (+50)
    ///   4 - M01 Terrain deformation (-2)
    ///   5 - M02 Collision (-5)
    ///   6 - M03 Out of course (-1)
    ///   7 - M04 Course lap (-3)
    ///   0 - Burst: emits one of each with a 0.1s gap
    /// </summary>
    public class EventLogDummyTrigger : MonoBehaviour
    {
        [Tooltip("Set to false to disable the debug keys entirely without removing the component.")]
        public bool enableKeys = true;

        private void Update()
        {
            if (!enableKeys) return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                Fire(GlobalVariables.ScoreEventId.P01, 3);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                Fire(GlobalVariables.ScoreEventId.P02, 5);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                Fire(GlobalVariables.ScoreEventId.P03, 50);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                Fire(GlobalVariables.ScoreEventId.M01, -2);
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                Fire(GlobalVariables.ScoreEventId.M02, -5);
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                Fire(GlobalVariables.ScoreEventId.M03, -1);
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                Fire(GlobalVariables.ScoreEventId.M04, -3);
            else if (Input.GetKeyDown(KeyCode.Alpha0))
                StartCoroutine(Burst());
        }

        private System.Collections.IEnumerator Burst()
        {
            var ids = new[]
            {
                (GlobalVariables.ScoreEventId.P01, 3),
                (GlobalVariables.ScoreEventId.P02, 5),
                (GlobalVariables.ScoreEventId.P03, 50),
                (GlobalVariables.ScoreEventId.M01, -2),
                (GlobalVariables.ScoreEventId.M02, -5),
                (GlobalVariables.ScoreEventId.M03, -1),
                (GlobalVariables.ScoreEventId.M04, -3),
            };
            foreach (var (id, pt) in ids)
            {
                Fire(id, pt);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private static void Fire(GlobalVariables.ScoreEventId id, int point)
        {
            var evt = new GlobalVariables.ScoreEvent { Id = id, Point = point };
            GlobalVariables.RegisterScoreEvent(evt);
            Debug.Log($"[EventLogDummyTrigger] fired {id} {point:+0;-0}pt");
        }
    }
}
