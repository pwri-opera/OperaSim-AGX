using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Temporary: enters Play mode when invoked from the command line
// (-executeMethod VideoAutoPlay.Enter) so the game view can be
// screen-recorded for the mission timelapse video. ActionMode=3 is
// driven by the runtime AutoPlaySim component via RuntimeInitializeOnLoadMethod.
public static class VideoAutoPlay
{
    public static void Enter()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.EnterPlaymode();
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
        Debug.Log("[VideoAutoPlay] EnterPlaymode requested");
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        EditorApplication.playModeStateChanged -= OnPlayMode;

        // Float the Game view as a screen-filling window so the recording
        // captures the IC120 view rather than the small docked panel.
        var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gvType != null)
        {
            var w = EditorWindow.GetWindow(gvType, utility: true);
            w.position = new Rect(0, 0, 1680, 1050);
        }

        // Attach the IC120 chase camera.
        var go = new GameObject("IC120ChaseCam");
        var cam = go.AddComponent<Camera>();
        cam.farClipPlane = 2000f;
        go.AddComponent<IC120ChaseCam>();
        foreach (var c in Object.FindObjectsOfType<Camera>())
            if (c != cam)
                c.enabled = false;
        Debug.Log("[VideoAutoPlay] floating game view + chase cam attached");
    }
}
