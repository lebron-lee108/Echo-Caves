using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace VP515.Editor
{
    /// <summary>Explicitly invoked, repeatable preparation. Never runs automatically.</summary>
    public static class ProjectPreparation
    {
        public const string ScenePath = "Assets/_VP515/Scenes/Foundation.unity";
        private const int PixelsPerUnit = 24;
        private const int ReferenceWidth = 960;
        private const int ReferenceHeight = 540;

        private static readonly string[] Folders =
        {
            "Assets/_VP515/Scenes", "Assets/_VP515/Scenes/Sandboxes",
            "Assets/_VP515/Scripts/Core", "Assets/_VP515/Scripts/Player",
            "Assets/_VP515/Scripts/Gameplay", "Assets/_VP515/Scripts/Presentation",
            "Assets/_VP515/Art/Pixel/Characters", "Assets/_VP515/Art/Pixel/Tiles",
            "Assets/_VP515/Art/Backgrounds", "Assets/_VP515/Art/UI",
            "Assets/_VP515/Animations", "Assets/_VP515/Materials",
            "Assets/_VP515/Audio/Music", "Assets/_VP515/Audio/SFX",
            "Assets/_VP515/Prefabs/Characters", "Assets/_VP515/Prefabs/Environment",
            "Assets/_VP515/Prefabs/UI", "Assets/_VP515/Data", "Assets/_VP515/Settings"
        };

        private static readonly string[] SortingLayers =
        {
            "Default", "Sky", "DistantBackground", "FarBackground", "MidBackground",
            "WorldBack", "Ground", "Actors", "Effects", "Foreground", "UI"
        };

        [MenuItem("VP515/Preparation/Apply baseline settings")]
        public static void Apply()
        {
            if (Application.unityVersion != "6000.3.23f1")
                throw new InvalidOperationException("Open VP515 with Unity 6000.3.23f1.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before preparing the project.");
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null || !(pipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("Universal 2D/URP must be active before preparation.");

            foreach (string folder in Folders) EnsureFolder(folder);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            QualitySettings.antiAliasing = 0;
            PlayerSettings.productName = "VP515";
            PlayerSettings.companyName = "VP515 Team";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            ConfigureLayers();

            if (!File.Exists(ScenePath)) CreateFoundation();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[VP515] Project preparation completed.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void ConfigureLayers()
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var sorting = manager.FindProperty("m_SortingLayers");
            foreach (string name in SortingLayers)
            {
                bool exists = false;
                for (int i = 0; i < sorting.arraySize; i++)
                    if (sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                        exists = true;
                if (exists) continue;
                int index = sorting.arraySize;
                sorting.InsertArrayElementAtIndex(index);
                var entry = sorting.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("name").stringValue = name;
                entry.FindPropertyRelative("uniqueID").intValue = 515000 + index;
                entry.FindPropertyRelative("locked").boolValue = false;
            }
            var layers = manager.FindProperty("layers");
            string[] physicsNames = { "Player", "Butterfly", "Shell", "TerrainSolid", "TerrainData", "Interactable", "Enemy", "GameplayTrigger" };
            for (int i = 0; i < physicsNames.Length; i++)
            {
                var layer = layers.GetArrayElementAtIndex(8 + i);
                if (string.IsNullOrEmpty(layer.stringValue)) layer.stringValue = physicsNames[i];
            }
            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateFoundation()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ReferenceHeight / (2f * PixelsPerUnit);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.10f, 0.16f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.allowDynamicResolution = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var pixels = cameraObject.AddComponent<PixelPerfectCamera>();
            pixels.assetsPPU = PixelsPerUnit;
            pixels.refResolutionX = ReferenceWidth;
            pixels.refResolutionY = ReferenceHeight;
            pixels.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixels.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            var background = new GameObject("Backgrounds").transform;
            foreach (string layer in new[] { "Sky", "DistantBackground", "FarBackground", "MidBackground" })
                new GameObject(layer).transform.SetParent(background, false);
            var gameplay = new GameObject("Gameplay").transform;
            var grid = new GameObject("Level Grid", typeof(Grid));
            grid.transform.SetParent(gameplay, false);
            var ground = new GameObject("Ground Tilemap", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            ground.transform.SetParent(grid.transform, false);
            ground.layer = LayerMask.NameToLayer("TerrainSolid");
            ground.GetComponent<TilemapRenderer>().sortingLayerName = "Ground";
            new GameObject("Actors").transform.SetParent(gameplay, false);
            new GameObject("Interactables").transform.SetParent(gameplay, false);
            new GameObject("Foreground");
            new GameObject("Atmosphere");
            new GameObject("UI Root");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Foundation scene could not be saved.");
        }

        [MenuItem("VP515/Preparation/Validate baseline")]
        public static void Validate()
        {
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Foundation scene is missing.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>()).Single();
            var pixels = camera.GetComponent<PixelPerfectCamera>();
            if (!camera.orthographic || pixels == null || pixels.assetsPPU != PixelsPerUnit)
                throw new InvalidOperationException("Camera preparation is incomplete.");
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null || urp.scriptableRenderer.GetType().Name != "Renderer2D")
                throw new InvalidOperationException("The project must use URP Renderer2D.");
            if (EditorSettings.serializationMode != SerializationMode.ForceText || UnityEditor.VersionControlSettings.mode != "Visible Meta Files")
                throw new InvalidOperationException("Unity Git collaboration settings are incomplete.");
            if (!File.Exists("Packages/packages-lock.json")) throw new InvalidOperationException("Package lock file is missing.");
            Directory.CreateDirectory("Docs/Setup");
            var report = new Report
            {
                editor = Application.unityVersion, scene = ScenePath, renderer = "URP Renderer2D",
                referenceWidth = ReferenceWidth, referenceHeight = ReferenceHeight, pixelsPerUnit = PixelsPerUnit,
                forceText = true, visibleMeta = true, baselineValidated = true
            };
            File.WriteAllText("Docs/Setup/baseline-validation.json", JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("[VP515] Baseline validation passed.");
        }

        [MenuItem("VP515/Preparation/Build Windows baseline")]
        public static void BuildWindowsBaseline()
        {
            Validate();
            Directory.CreateDirectory("Builds/Windows/Baseline");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/Baseline/VP515.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Windows baseline build failed: " + report.summary.result);
            File.WriteAllText("Docs/Setup/windows-build-validation.json",
                "{\n  \"target\": \"StandaloneWindows64\",\n  \"result\": \"Succeeded\",\n  \"errors\": " + report.summary.totalErrors +
                ",\n  \"warnings\": " + report.summary.totalWarnings +
                ",\n  \"scope\": \"Foundation scene only; no gameplay\"\n}\n");
            Debug.Log("[VP515] Windows baseline build succeeded.");
        }

        [Serializable]
        private sealed class Report
        {
            public string editor, scene, renderer;
            public int referenceWidth, referenceHeight, pixelsPerUnit;
            public bool forceText, visibleMeta, baselineValidated;
        }
    }
}
