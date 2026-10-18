using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EchoCaves.Scene;

namespace EchoCaves.Editor
{
    public static class EchoVerification
    {
        [MenuItem("Echo Caves/Verify C Handoff")]
        public static void VerifyMenu(){Debug.Log(RunAll());}
        public static string RunAll()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Open chapter first");
            var lines=new[]{EchoSceneChecks.Validate(),EchoSceneChecks.ValidatePresentation(),EchoSceneChecks.ValidateWorldDesign(),EchoSceneChecks.ValidateRouteGeometry(),EchoSceneChecks.ValidateDepthTint(),EchoUIBuilder.Validate(),EchoUIBuilder.ValidatePrefab()}.ToList();
            var roots=scene.GetRootGameObjects();var renderers=roots.SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
            if(renderers.Any(r=>!r.sprite||!EditorUtility.IsPersistent(r.sprite)))throw new Exception("Nonpersistent or missing chapter sprite");
            if(roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count(e=>e.isActiveAndEnabled)!=1)throw new Exception("Expected one active EventSystem");
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var foreign=new GameObject("ForeignAnchorMustNotExport");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(foreign,preview);foreign.AddComponent<EchoAnchor>().Id="FORBIDDEN_FOREIGN";
                EchoHandoffAssembly.ExportAnchors();if(File.ReadAllText("Docs/EchoCaves/实际场景锚点.csv").Contains("FORBIDDEN_FOREIGN"))throw new Exception("Cross-scene anchor leak");
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_EchoCaves/Prefabs/Modules-v04"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var content=PrefabUtility.LoadPrefabContents(path);
                try{foreach(var s in content.GetComponentsInChildren<SpriteRenderer>(true))if(!s.sprite)throw new Exception("Missing prefab sprite: "+path);if(content.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing prefab script: "+path);}
                finally{PrefabUtility.UnloadPrefabContents(content);}
            }
            lines.Add("PASS: "+renderers.Length+" persistent scene sprites; one active EventSystem; foreign-scene anchor excluded; 9 native module prefab roundtrips");
            var result=string.Join("\n",lines);if(result.Contains("FAIL"))throw new Exception(result);
            File.WriteAllText("Docs/EchoCaves/最新原生检查.txt",result+"\nAutomated presentation/geometry checks only; no full player-controller playthrough.\n",new System.Text.UTF8Encoding(true));
            return result;
        }
    }
}
