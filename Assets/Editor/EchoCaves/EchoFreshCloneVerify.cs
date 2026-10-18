using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EchoCaves.Editor
{
    public static class EchoFreshCloneVerify
    {
        // Batch-mode entry point. Always opens the serialized delivery; never rebuilds it from local art paths.
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene(EchoSceneChecks.ScenePath);
                var report=EchoVerification.RunAll();
                File.WriteAllText("FreshClone-Verification.txt",report);
                UnityEngine.Debug.Log("ECHO_FRESH_CLONE_PASS\n"+report);
                EditorApplication.Exit(0);
            }
            catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
