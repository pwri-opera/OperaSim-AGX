using UnityEngine;

// Temporary: chase camera that tracks the IC120 body (track_link) during
// mission recording. Spawned by AutoPlaySim; disables all other cameras
// and renders full-screen from above/behind the machine.
public class IC120ChaseCam : MonoBehaviour
{
    private Transform target;   // ic120_0/track_link
    private Camera cam;

    void LateUpdate()
    {
        if (target == null)
        {
            var t = GameObject.Find("track_link");
            if (t == null)
                return;
            target = t.transform;
        }
        var p = target.position;
        // trailing position ~25 m behind and ~18 m above, looking at the body
        var back = Quaternion.Euler(0, target.eulerAngles.y, 0) * Vector3.back * 25f;
        transform.position = p + back + Vector3.up * 18f;
        transform.rotation = Quaternion.LookRotation(p - transform.position + Vector3.forward * 0.001f, Vector3.up);
    }
}
