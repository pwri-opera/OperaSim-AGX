using System.IO;
using UnityEngine;

// Temporary: auto-starts the simulation in Play mode when the process was
// launched with -executeMethod VideoAutoPlay.Enter. Enables the AGX physics
// gate by driving GlobalVariables.changeActionMode(3) the same way the HUD
// "Start Simulation" button does.
//
// Additionally, while launched via VideoAutoPlay:
// - spawns a chase camera parented to ic120_0/track_link so the Game view
//   follows the truck,
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
        if (chaseCam != null)
            return;
        GameObject track = GameObject.Find("ic120_0/track_link");
        if (track == null)
        {
            foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            {
                if (t.name == "track_link")
                {
                    track = t.gameObject;
                    break;
                }
            }
        }
        if (track == null)
            return;
        var camGo = new GameObject("VideoChaseCam");
        camGo.transform.SetParent(track.transform, false);
        camGo.transform.localPosition = new Vector3(-14f, 6.5f, 0f);
        camGo.transform.localRotation = Quaternion.LookRotation(new Vector3(12f, -4.5f, 0f), Vector3.up);
        chaseCam = camGo.AddComponent<Camera>();
        chaseCam.fieldOfView = 42f;
        chaseCam.depth = 10; // wins over the scene's HUD/ground camera
        rt = new RenderTexture(1920, 1080, 24);
        chaseCam.targetTexture = rt;
        outDir = Path.Combine(Application.dataPath, "../.omp/video/frames2");
        Directory.CreateDirectory(outDir);
        Debug.Log("[AutoPlaySim] chase cam on track_link; dumping " + outDir);
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
