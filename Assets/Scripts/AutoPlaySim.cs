using System.IO;
using UnityEngine;

// Temporary: auto-starts the simulation in Play mode when the process was
// launched with -executeMethod VideoAutoPlay.Enter. Enables the AGX physics
// gate by driving GlobalVariables.changeActionMode(3) the same way the HUD
// "Start Simulation" button does.
//
// Additionally, while launched via VideoAutoPlay:
// - spawns a chase camera that is reposed in world space behind the ic120_0
//   body (track_link) every frame so the Game view follows the truck,
// - captures that camera's render to a 1920x1080 RenderTexture and dumps PNG
//   frames at 5 fps to .omp/video/frames2/ for ffmpeg stitching (works at
//   full resolution regardless of the editor window's Game view size).
public class AutoPlaySim : MonoBehaviour
{
    private const int StartDelayFrames = 120;
    private const float FrameInterval = 0.2f;

    private int frames;
    private bool started;
    private Camera chaseCam;
    private Transform trackLink;
    private RenderTexture rt;
    private string outDir;
    private float nextCapture;
    private int frameIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (!System.Environment.CommandLine.Contains("-executeMethod VideoAutoPlay.Enter"))
            return;
        var go = new GameObject("AutoPlaySim");
        go.AddComponent<AutoPlaySim>();
    }

    private void TryAttach()
    {
        if (trackLink == null)
        {
            GameObject track = GameObject.Find("ic120_0/track_link");
            if (track == null)
            {
                foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
                {
                    if (t.name == "track_link" &&
                        t.root != null && t.root.name.StartsWith("ic120"))
                    {
                        track = t.gameObject;
                        break;
                    }
                }
            }
            if (track == null)
                return;
            trackLink = track.transform;
        }
        if (chaseCam == null)
        {
            var camGo = new GameObject("VideoChaseCam");
            chaseCam = camGo.AddComponent<Camera>();
            chaseCam.fieldOfView = 42f;
            rt = new RenderTexture(1920, 1080, 24);
            chaseCam.targetTexture = rt;
            outDir = Path.Combine(Application.dataPath, "../.omp/video/frames2");
            Directory.CreateDirectory(outDir);
            Debug.Log("[AutoPlaySim] chase cam on " + trackLink.root.name +
                "/" + trackLink.name + "; dumping " + outDir);
        }
        // Repose every frame instead of parenting. The link's joint frame is
        // rotated 90 deg vs the body (parenting produced a sideways chase
        // cam); the body's horizontal forward is +link.right. Root
        // ('ic120_0') never moves and cannot be used for yaw.
        Vector3 flatFwd = trackLink.right;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude < 1e-4f)
            flatFwd = Vector3.forward;
        flatFwd.Normalize();
        Vector3 pos = trackLink.position - flatFwd * 16f + Vector3.up * 8f;
        chaseCam.transform.position = pos;
        chaseCam.transform.rotation = Quaternion.LookRotation(
            (trackLink.position + flatFwd * 4f + Vector3.up * 1.5f) - pos,
            Vector3.up);
    }

    void Update()
    {
        frames++;
        TryAttach();
        if (!started && frames >= StartDelayFrames)
        {
            var m = typeof(PWRISimulator.GlobalVariables).GetMethod("changeActionMode",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m != null)
                m.Invoke(null, new object[] { 3 });
            else
                typeof(PWRISimulator.GlobalVariables).GetField("ActionMode",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .SetValue(null, 3);
            Debug.Log("[AutoPlaySim] ActionMode=3 set (simulation start)");
            started = true;
        }
    }

    void LateUpdate()
    {
        if (chaseCam == null || Time.time < nextCapture)
            return;
        nextCapture = Time.time + FrameInterval;
        chaseCam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(outDir, $"f{frameIndex++:D6}.png"), tex.EncodeToPNG());
        Destroy(tex);
    }
}
