using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace VP515.Editor
{
    // First written against the empty Foundation scene. Missing slice must fail.
    public static class SliceIntegrationChecks
    {
        public static string Run()
        {
            var failures = new List<string>();
            Check(GameObject.Find("ChapterOneSlice") != null, "slice root exists", failures);
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<VP515.Slice.SliceBootstrap>();
            Check(bootstrap && bootstrap.CameraRig, "camera adapter survives serialization", failures);
            Check(SceneManager.GetActiveScene().name == "ChapterOneSlice", "slice scene active", failures);
            Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length == 1, "one game camera", failures);
            Check(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length > 0, "real Canvas exists", failures);
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "one input EventSystem", failures);
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(buttons.Length >= 10, "functional menu/pause/backpack/end buttons exist", failures);
            var collision = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            Check(collision.Length >= 8, "world has collidable terrain and actors", failures);
            Check(!EditorUtility.scriptCompilationFailed, "editor compiler clear", failures);
            if (failures.Count > 0) throw new InvalidOperationException("SLICE INTEGRATION FAIL: " + string.Join("; ", failures));
            return "PASS: 9 scene integration checks";
        }

        private static void Check(bool valid, string name, List<string> failures)
        {
            if (!valid) failures.Add(name);
        }
    }
}
