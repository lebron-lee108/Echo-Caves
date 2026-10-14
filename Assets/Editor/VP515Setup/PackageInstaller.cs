using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace VP515.Editor
{
    // Command-line only. Package Manager resolves dependencies, never direct manifest edits.
    public static class PackageInstaller
    {
        private static AddAndRemoveRequest request;
        private static double deadline;

        public static void Install()
        {
            request = Client.AddAndRemove(new[] { "com.unity.pipeline@0.8.0-exp.1" }, new string[0]);
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup <= deadline) return;
                EditorApplication.update -= Poll;
                Debug.LogError("[VP515] Package installation timed out.");
                EditorApplication.Exit(2);
                return;
            }
            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
            {
                Debug.Log("[VP515] Unity Pipeline package resolved by Package Manager.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[VP515] Package installation failed: " + request.Error?.message);
                EditorApplication.Exit(1);
            }
        }
    }
}
