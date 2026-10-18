#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using EchoCaves.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EchoCaves.Editor
{
    /// <summary>Explicitly invoked authoring only. No automatic refresh, scene save or global asset save.</summary>
    public static class EchoUIBuilder
    {
        public const string FontSourcePath = "Assets/_EchoCaves/Fonts/NotoSansCJKsc-Regular.otf";
        public const string FontAssetPath = "Assets/_EchoCaves/UI/Fonts/EchoCavesUI-SDF.asset";
        public const string PrefabPath = "Assets/_EchoCaves/UI/Prefabs/EchoCavesUI-v03.prefab";
        private static readonly Color Navy = Hex("081728");
        private static readonly Color Brass = Hex("c39a57");
        private static readonly Color Cyan = Hex("57dbe5");
        private static readonly Color Ivory = Hex("f2edda");
        private static readonly Color Muted = Hex("8b9aa9");
        private static readonly Color Panel = Hex("0b2035");

        public static GameObject Build(Transform parent = null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (source == null) throw new InvalidOperationException("Copy/import the supplied font first: " + FontSourcePath);
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
                throw new InvalidOperationException("TMP SDF shader missing. Main agent must prepare TMP resources noninteractively.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            bool newFont = font == null;
            if (newFont)
            {
                EnsureFolder("Assets/_EchoCaves/UI/Fonts");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (font == null) throw new InvalidOperationException("TMP font creation failed.");
                font.name = "EchoCavesUI-SDF";
                AssetDatabase.CreateAsset(font, FontAssetPath);
            }
            GameObject root = null;
            try
            {
                root = new GameObject("EchoCavesUI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                if (parent != null) root.transform.SetParent(parent, false);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvas.pixelPerfect = true;
                var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600, 900);
                scaler.matchWidthOrHeight = .5f;
                var bindings = root.AddComponent<EchoUIBindings>();
                new Author(root, bindings, font).CreateAll();
                if (newFont)
                {
                    // Bake this authored vocabulary; keep Dynamic for later Inspector copy edits.
                    string vocabulary = string.Concat(root.GetComponentsInChildren<TextMeshProUGUI>(true).Select(t => t.text));
                    font.TryAddCharacters(vocabulary, out string missing);
                    if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Echo UI font missing characters: " + missing);
                    if (font.material != null && !EditorUtility.IsPersistent(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
                    foreach (var texture in font.atlasTextures)
                        if (texture != null && !EditorUtility.IsPersistent(texture)) AssetDatabase.AddObjectToAsset(texture, font);
                    EditorUtility.SetDirty(font);
                    AssetDatabase.SaveAssetIfDirty(font);
                    if (font.material != null) AssetDatabase.SaveAssetIfDirty(font.material);
                    foreach (var texture in font.atlasTextures) if (texture != null) AssetDatabase.SaveAssetIfDirty(texture);
                }
                EnsureEventSystem(root);
                return root;
            }
            catch
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        public static void BuildPrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Prefab already exists; preserve manual authoring. Choose a new path or explicitly remove your generated prefab first: " + PrefabPath);
            EnsureFolder("Assets/_EchoCaves/UI/Prefabs");
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject host = null;
            try
            {
                host = new GameObject("EchoUIAuthoringHost");
                SceneManager.MoveGameObjectToScene(host, preview);
                var root = Build(host.transform);
                EchoUIContractTests.AssertRoot(root);
                EchoUIContractTests.AssertNavigation(root);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || saved == null) throw new InvalidOperationException("Echo UI prefab save failed.");
                Debug.Log(ValidatePrefab());
                Debug.Log("Echo UI prefab authored: " + PrefabPath);
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        public static string ValidatePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("Generate the UI prefab first: " + PrefabPath);
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { return "Prefab round trip: " + EchoUIContractTests.AssertRoot(contents) + "\n" + EchoUIContractTests.AssertNavigation(contents); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        /// <summary>Native integration contract, in a disposable preview scene. Does not save a scene/prefab.</summary>
        public static string Validate()
        {
            var sharedScene = SceneManager.GetActiveScene();
            bool sharedDirty = sharedScene.isDirty;
            var sharedRoots = sharedScene.GetRootGameObjects().Select(g => g.GetInstanceID()).OrderBy(id => id).ToArray();
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject host = null;
            try
            {
                host = new GameObject("EchoUIValidationHost");
                host.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(host, preview);
                var root = Build(host.transform);
                root.name = "EchoUIValidation_Disposable_NotShared";
                root.hideFlags = HideFlags.DontSave;
                string summary = EchoUIContractTests.AssertRoot(root) + "\n" + EchoUIContractTests.AssertNavigation(root);
#if ENABLE_INPUT_SYSTEM
                summary += "\n" + EchoUIContractTests.AssertInputRebinding(root);
#endif
                summary += "\n" + ValidateTextLayout(root);
                var systems = root.GetComponentsInChildren<EventSystem>(true);
                if (systems.Length != 1) throw new InvalidOperationException("Isolated prefab needs exactly one EventSystem.");
                if (systems[0].GetComponent<BaseInputModule>() == null) throw new InvalidOperationException("Input module missing.");
                return summary + "\nPASS: text bounds and isolated EventSystem. Visual capture still required.";
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                EditorSceneManager.ClosePreviewScene(preview);
                if (SceneManager.GetActiveScene() != sharedScene || sharedScene.isDirty != sharedDirty
                    || !sharedScene.GetRootGameObjects().Select(g => g.GetInstanceID()).OrderBy(id => id).SequenceEqual(sharedRoots))
                    throw new InvalidOperationException("Validation changed the shared scene identity, root set or dirty state.");
            }
        }

        /// <summary>Measure every label while its own screen is active, reporting all failures in one pass.</summary>
        public static string ValidateTextLayout(GameObject root)
        {
            if (root == null || !EditorSceneManager.IsPreviewScene(root.scene))
                throw new InvalidOperationException("Text layout audit requires a disposable preview root, never shared scene UI.");
            var bindings = root.GetComponent<EchoUIBindings>();
            var hud = root.transform.Find("HUD");
            var hudExamples = new[] { "BossStagger", "RecordingSnapshot", "FeedbackSnapshot", "PreviewNavigation" }
                .Select(name => hud.Find(name)?.gameObject).ToArray();
            if (hudExamples.Any(example => example == null)) throw new InvalidOperationException("Missing serialized HUD example group.");
            var failures = new List<string>();
            int count = 0;
            try
            {
                foreach (EchoUIView id in Enum.GetValues(typeof(EchoUIView)))
                {
                    bindings.ShowView((int)id);
                    if (id == EchoUIView.HUD)
                        foreach (var example in hudExamples) example.SetActive(true);
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(false))
                    {
                        label.ForceMeshUpdate();
                        Vector2 size = label.rectTransform.rect.size;
                        Vector2 required = label.GetPreferredValues(label.text, size.x, float.PositiveInfinity);
                        count++;
                        if (label.isTextOverflowing || required.x > size.x + .5f || required.y > size.y + .5f)
                            failures.Add(id + "/" + label.transform.parent.name + "/" + label.name
                                + " font=" + label.fontSize.ToString("0.##") + " rect=" + size.ToString("F1")
                                + " required=" + required.ToString("F1") + " lines=" + label.textInfo.lineCount);
                    }
                    if (id == EchoUIView.HUD)
                        foreach (var example in hudExamples) example.SetActive(false);
                }
            }
            finally
            {
                foreach (var example in hudExamples) example.SetActive(false);
                bindings.ShowView((int)EchoUIView.HUD);
            }
            EchoUIContractTests.AssertCleanHUD(root);
            if (failures.Count > 0) throw new InvalidOperationException("Text overflow (" + failures.Count + "):\n" + string.Join("\n", failures));
            return "PASS: " + count + " labels measured across 13 active views (including hidden HUD examples); no text overflow; clean HUD restored.";
        }

        /// <summary>Deferred main-agent migration for an already-authored UI. No rebuild, view switch or scene save.</summary>
        public static void ApplyTextLayoutFixes(GameObject root)
        {
            if (root == null || root.GetComponent<EchoUIBindings>() == null || EditorUtility.IsPersistent(root))
                throw new ArgumentException("Supply the existing scene Echo UI root.", nameof(root));
            var menuTitle = root.transform.Find("MainMenu/Wordmark")?.GetComponent<TextMeshProUGUI>();
            var heights = new Dictionary<string, float> {
                {"HUD/Vitals",160}, {"HUD/Vitals/Abilities",56}, {"HUD/Objective/Heading",36},
                {"MainMenu/Wordmark",menuTitle != null && menuTitle.fontSize > 70 ? 140 : 110}, {"Pause/PauseCard/Title",68}, {"Dialogue/DialogueCard/Speaker",44},
                {"SoundLibrary/S01/SoundId",48}, {"SoundLibrary/S02/SoundId",48},
                {"SoundLibrary/S03/SoundId",48}, {"SoundLibrary/S04/SoundId",48},
                {"Radio/SinglePlayerSoundscape/Recording",52}, {"Radio/SinglePlayerSoundscape/Clock",42} };
            var targets = heights.Select(entry => (rect: root.transform.Find(entry.Key) as RectTransform,
                height: entry.Value, path: entry.Key)).ToArray();
            if (targets.Any(t => t.rect == null)) throw new InvalidOperationException("UI layout migration found missing paths; nothing changed.");
            Undo.RecordObjects(targets.Select(t => (UnityEngine.Object)t.rect).ToArray(), "Echo UI measured text bounds");
            foreach (var target in targets)
            {
                target.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target.height);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target.rect);
            }
        }

        private static void EnsureEventSystem(GameObject root)
        {
            bool exists = root.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EventSystem>(true))
                .Any(e => e.isActiveAndEnabled);
            if (exists) return;
            var go = new GameObject("PresentationEventSystem", typeof(EventSystem));
            go.SetActive(false);
            go.transform.SetParent(root.transform, false);
            var hud = root.GetComponent<EchoUIBindings>().Views.First(v => v.id == EchoUIView.HUD);
            if (hud.firstSelected != null) go.GetComponent<EventSystem>().firstSelectedGameObject = hud.firstSelected.gameObject;
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            ConfigurePreviewInput(module);
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            go.SetActive(true);
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>Explicit main-agent repair of this UI instance only; no scene/prefab save.</summary>
        public static void RepairPreviewInput(GameObject root)
        {
            if (root == null || root.GetComponent<EchoUIBindings>() == null)
                throw new ArgumentException("Supply the owned EchoCavesUI root.", nameof(root));
            foreach (var module in root.GetComponentsInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(true))
                ConfigurePreviewInput(module);
        }

        private static void ConfigurePreviewInput(UnityEngine.InputSystem.UI.InputSystemUIInputModule module)
        {
            const string path = "Assets/_EchoCaves/UI/Input/EchoUIPreviewInput.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
            if (asset == null)
            {
                EnsureFolder("Assets/_EchoCaves/UI/Input");
                asset = ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputActionAsset>();
                asset.name = "EchoUIPreviewInput";
                var map = new UnityEngine.InputSystem.InputActionMap("UI");
                asset.AddActionMap(map);
                map.AddAction("Point", UnityEngine.InputSystem.InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
                map.AddAction("Click", UnityEngine.InputSystem.InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
                map.AddAction("Scroll", UnityEngine.InputSystem.InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
                var move = map.AddAction("Navigate", UnityEngine.InputSystem.InputActionType.PassThrough, expectedControlLayout: "Vector2");
                move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                move.AddBinding("<Gamepad>/dpad");
                var submit = map.AddAction("Submit", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/enter");
                submit.AddBinding("<Gamepad>/buttonSouth");
                var cancel = map.AddAction("Cancel", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
                cancel.AddBinding("<Gamepad>/buttonEast");
                AssetDatabase.CreateAsset(asset, path);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            // Existing generated assets must also get any missing references without replacing the asset or IDs.
            var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.InputSystem.InputActionReference>().ToList();
            foreach (var action in asset.FindActionMap("UI", true).actions)
            {
                var reference = references.FirstOrDefault(r => r.asset == asset && r.action != null && r.action.id == action.id
                    && EditorUtility.IsPersistent(r) && AssetDatabase.IsSubAsset(r));
                if (reference == null)
                {
                    reference = UnityEngine.InputSystem.InputActionReference.Create(action);
                    reference.name = action.name;
                    AssetDatabase.AddObjectToAsset(reference, asset);
                    // Save this newly created subasset, even when the parent asset already existed.
                    EditorUtility.SetDirty(reference);
                    AssetDatabase.SaveAssetIfDirty(reference);
                    references.Add(reference);
                }
            }
            UnityEngine.InputSystem.InputActionReference Ref(string name) => references.First(r => r.asset == asset && r.action != null && r.action.name == name
                && EditorUtility.IsPersistent(r) && AssetDatabase.IsSubAsset(r));
            // actionsAsset setter remaps old slots via InputActionReference.Create (transient).
            // SwapAction then ignores a persistent replacement if it points to that same action.
            // Clear every old slot BEFORE assigning the asset so no transient remapping can occur.
            module.point = null; module.leftClick = null; module.scrollWheel = null;
            module.move = null; module.submit = null; module.cancel = null;
            module.rightClick = null; module.middleClick = null;
            module.trackedDevicePosition = null; module.trackedDeviceOrientation = null;
            module.actionsAsset = asset;
            module.point = Ref("Point"); module.leftClick = Ref("Click"); module.scrollWheel = Ref("Scroll");
            module.move = Ref("Navigate"); module.submit = Ref("Submit"); module.cancel = Ref("Cancel");
            EditorUtility.SetDirty(module);
            EchoUIContractTests.AssertInputReferences(module);
        }
#endif

        /// <summary>Deferred main-agent invocation only. Clone the existing UI; never rebuild or alter the main scene.</summary>
        public static string[] CaptureScreens()
        {
            var candidates = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<EchoUIBindings>(true)).ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException("Expected exactly one existing Echo UI root; pass the intended root to CaptureScreens(GameObject). No UI was built.");
            return CaptureScreens(candidates[0].gameObject);
        }

        /// <summary>Render a disposable clone of the supplied UI into Evidence/EchoCaves/UI; source stays untouched.</summary>
        public static string[] CaptureScreens(GameObject sourceRoot)
        {
            if (sourceRoot == null || sourceRoot.GetComponent<EchoUIBindings>() == null || sourceRoot.GetComponent<Canvas>() == null
                || EditorUtility.IsPersistent(sourceRoot))
                throw new ArgumentException("Supply an existing scene Echo UI Canvas instance.", nameof(sourceRoot));
            string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Evidence/EchoCaves/UI",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8)));
            return CaptureScreensCore(sourceRoot, directory);
        }

        /// <summary>Build and render thirteen UI-only views in a disposable scene. Never builds into the shared scene.</summary>
        public static string[] CaptureScreens(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Specify a new capture folder.", nameof(folder));
            return CaptureScreensCore(null, System.IO.Path.GetFullPath(folder));
        }

        // Pipeline run_script cannot select overloaded entry points; this adapter uses the same implementation.
        public static string[] CaptureScreensToFolder(string folder) => CaptureScreens(folder);

        private static string[] CaptureScreensCore(GameObject sourceRoot, string directory)
        {
            if (System.IO.Directory.Exists(directory) && System.IO.Directory.EnumerateFiles(directory, "*.png").Any())
                throw new InvalidOperationException("Capture folder already contains PNGs; choose a new folder to preserve evidence.");
            var sharedScene = SceneManager.GetActiveScene();
            bool sharedDirty = sharedScene.isDirty;
            var sharedRoots = sharedScene.GetRootGameObjects().Select(g => g.GetInstanceID()).OrderBy(id => id).ToArray();
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject host = null;
            RenderTexture target = null;
            Texture2D image = null;
            RenderTexture previousTarget = RenderTexture.active;
            var paths = new List<string>();
            try
            {
                host = new GameObject("EchoUICaptureHost");
                host.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(host, preview);
                var root = sourceRoot == null ? Build(host.transform) : UnityEngine.Object.Instantiate(sourceRoot, host.transform, false);
                root.name = "EchoUICapture_Disposable_NotShared";
                root.hideFlags = HideFlags.DontSave;
                root.transform.localScale = Vector3.one;
                root.SetActive(true);
                var bindings = root.GetComponent<EchoUIBindings>();
                bindings.ShowView((int)EchoUIView.HUD);
                foreach (var system in root.GetComponentsInChildren<EventSystem>(true)) system.gameObject.SetActive(false);
                var cameraObject = new GameObject("EchoUICaptureCamera", typeof(Camera));
                cameraObject.transform.SetParent(host.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.scene = preview;
                camera.orthographic = true;
                camera.orthographicSize = 450;
                camera.aspect = 1600f / 900f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Navy;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                camera.transform.localPosition = new Vector3(0, 0, -20);
                camera.cullingMask = 1 << 31;
                target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1, name = "EchoUICapture1600x900" };
                target.Create();
                camera.targetTexture = target;
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
                System.IO.Directory.CreateDirectory(directory);
                foreach (EchoUIView id in Enum.GetValues(typeof(EchoUIView)))
                {
                    bindings.ShowView((int)id);
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(false)) text.ForceMeshUpdate();
                    Canvas.ForceUpdateCanvases();
                    if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                    {
                        var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target };
                        if (!UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera, request))
                            throw new InvalidOperationException("Active SRP does not support StandardRequest; main must capture through its renderer.");
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                    }
                    else camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    image.Apply(false);
                    var pixels = image.GetPixels32();
                    if (!pixels.Any(pixel => !pixel.Equals(pixels[0])))
                        throw new InvalidOperationException("Capture rendered a uniform image: " + id);
                    string file = System.IO.Path.Combine(directory, ((int)id).ToString("00") + "-" + id + ".png");
                    System.IO.File.WriteAllBytes(file, image.EncodeToPNG());
                    paths.Add(file);
                }
                bindings.ShowView((int)EchoUIView.HUD);
                if (paths.Count != 13) throw new InvalidOperationException("Expected thirteen UI captures.");
                Debug.Log("Echo UI: 13 UI-only native renders (no world scene), " + directory);
                return paths.ToArray();
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                EditorSceneManager.ClosePreviewScene(preview);
                if (SceneManager.GetActiveScene() != sharedScene || sharedScene.isDirty != sharedDirty
                    || !sharedScene.GetRootGameObjects().Select(g => g.GetInstanceID()).OrderBy(id => id).SequenceEqual(sharedRoots))
                    throw new InvalidOperationException("Capture changed the shared scene identity, root set or dirty state.");
            }
        }

        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }

        private sealed class Author
        {
            private readonly GameObject root;
            private readonly EchoUIBindings b;
            private readonly TMP_FontAsset font;
            private readonly List<EchoUIViewReference> views = new List<EchoUIViewReference>();
            public Author(GameObject root, EchoUIBindings bindings, TMP_FontAsset font) { this.root = root; b = bindings; this.font = font; }

            public void CreateAll()
            {
                HUD(); MainMenu(); Pause(); Settings(); Dialogue(); Map(); SoundLibrary(); Playback(); Radio(); Shop(); Settlement(); Fail(); End();
                b.ConfigureViews(views.ToArray());
                foreach (var view in views) view.panel.SetActive(view.id == EchoUIView.HUD);
            }

            private RectTransform View(EchoUIView id, string title, bool opaque = true)
            {
                var rt = Rect(id.ToString(), root.transform, 0, 0, 1600, 900);
                Stretch(rt);
                if (opaque) { var bg = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); bg.color = Navy; bg.raycastTarget = true; }
                views.Add(new EchoUIViewReference { id = id, panel = rt.gameObject });
                if (opaque)
                {
                    Frame(rt, 28, 26, 1544, 848);
                    Label(rt, "ScreenTitle", title, 60, 54, 1400, 58, 32, Ivory);
                    Box(rt, "TitleRule", 60, 125, 1480, 1, Brass);
                    Label(rt, "PreviewBadge", "界面预览 / UI PREVIEW · 静态样例 · 功能待 B 接入", 60, 824, 1300, 32, 18, Muted);
                }
                return rt;
            }

            private void HUD()
            {
                var p = View(EchoUIView.HUD, "", false);
                var status = Card(p, "Vitals", 32, 30, 340, 160);
                Label(status, "HealthSample", "体力  ♥  ♥  ♥", 18, 12, 300, 35, 24, Ivory);
                Meter(status, "EnergySample", 18, 64, 220, 12, 1f);
                b.energyLabel = Label(status, "EnergyLabel", "100 / 100", 244, 53, 82, 35, 17, Cyan);
                Label(status, "Abilities", "1 推离波   2 敲地波（示例）\nQ / E 扫描 · 冷却样例", 18, 91, 304, 56, 17, Ivory);
                var objective = Card(p, "Objective", 1200, 30, 368, 148);
                Label(objective, "Heading", "当前目标 / OBJECTIVE", 20, 14, 326, 36, 21, Brass);
                b.objectiveLabel = Label(objective, "ObjectiveLabel", "录下灯尾蛾\n靠近目标，按住 F 录音。", 20, 53, 326, 72, 22, Ivory);
                var boss = Card(p, "BossStagger", 575, 30, 450, 94);
                Label(boss, "StaggerExplanation", "失衡示例 · 三次有效反击后驱离", 16, 8, 418, 28, 19, Ivory);
                var marks = Rect("Markers", boss, 169, 48, 112, 26);
                for (int i = 0; i < 3; i++) Box(marks, "WhiteSquare" + (i + 1), i * 42, 0, 24, 24, Color.white);
                b.bossMarkers = marks;
                var recording = Card(p, "RecordingSnapshot", 32, 664, 365, 124);
                Label(recording, "Label", "交互反馈样例 / RECORDING", 16, 8, 330, 28, 18, Brass);
                Label(recording, "RecordingProgressLabel", "按住 F · 录音中…（静态 65%）", 16, 42, 330, 32, 19, Ivory);
                Meter(recording, "Progress", 16, 91, 330, 10, .65f);
                var feedback = Card(p, "FeedbackSnapshot", 1200, 664, 368, 124);
                b.feedbackLabel = Label(feedback, "FeedbackLabel", "已录入：S01 灯尾蛾群鸣\n录音不耗能 · 母带保留\n能量不足：扫描 / 回放 / 攻击受限", 16, 10, 336, 100, 19, Ivory);
                var bar = Card(p, "PreviewNavigation", 32, 802, 1536, 68);
                string[] names = { "菜单", "暂停", "对话", "地图", "母带库", "原声", "电台", "商店", "结算", "倒下", "章末" };
                EchoUIView[] ids = { EchoUIView.MainMenu, EchoUIView.Pause, EchoUIView.Dialogue, EchoUIView.Map, EchoUIView.SoundLibrary, EchoUIView.Playback, EchoUIView.Radio, EchoUIView.Shop, EchoUIView.Settlement, EchoUIView.Fail, EchoUIView.End };
                Label(bar, "Badge", "预览", 14, 16, 65, 36, 18, Cyan);
                var row = Rect("Buttons", bar, 88, 12, 1432, 44);
                Horizontal(row, 8);
                for (int i = 0; i < ids.Length; i++) Nav(row, "Preview" + ids[i], names[i], 0, 0, 120, 44, ids[i]);
                // B owns when these states appear. Main's review component may toggle the dock with F1.
                boss.gameObject.SetActive(false);
                recording.gameObject.SetActive(false);
                feedback.gameObject.SetActive(false);
                bar.gameObject.SetActive(false);
            }

            private void MainMenu()
            {
                // Board 18: editable UI overlays the chapter camera; never substitute a baked board or waveform for cave art.
                var p = View(EchoUIView.MainMenu, "", false);
                Box(p, "LeftBacking", 0, 0, 720, 900, new Color(Navy.r, Navy.g, Navy.b, .82f));
                Frame(p, 28, 26, 1544, 848);
                Label(p, "Wordmark", "回声矿井", 140, 190, 550, 140, 80, Ivory);
                Label(p, "Subtitle", "E C H O   C A V E S", 160, 332, 450, 46, 26, Ivory);
                Action(p, "ContinueGame", "继续游戏 / Continue", 160, 420, 450, 58, b.RequestContinueGame);
                Action(p, "NewGame", "开始新游戏 / New game", 160, 494, 450, 58, b.RequestNewGame);
                Action(p, "OpenSettings", "设置 / Settings", 160, 568, 450, 58, b.OpenSettings);
                Action(p, "Quit", "退出 / Quit", 160, 642, 450, 58, b.RequestQuit);
                Nav(p, "PreviewExplore", "HUD 预览 →", 390, 797, 220, 36, EchoUIView.HUD);
                Label(p, "PreviewBadge", "界面预览 / UI PREVIEW · 功能待 B 接入", 50, 844, 640, 24, 14, Muted);
            }

            private void Pause()
            {
                var p = View(EchoUIView.Pause, "暂停 / PAUSE");
                var c = Card(p, "PauseCard", 470, 210, 660, 450);
                Label(c, "Title", "暂时歇一会儿", 40, 34, 580, 68, 42, Ivory, TextAlignmentOptions.Center);
                Action(c, "ResumeRequested", "继续游戏 / Resume · 待接入", 100, 129, 460, 58, b.RequestResume);
                SettingsButton(c, 100, 208, 460);
                Action(c, "MenuRequested", "返回主菜单 / Main menu · 待接入", 100, 287, 460, 58, b.RequestReturnToMenu);
                Label(c, "Scope", "此处只预览暂停页面；不会更改游戏计时。", 40, 374, 580, 42, 20, Muted, TextAlignmentOptions.Center);
                Nav(p, "PreviewBack", "返回 HUD 预览", 590, 704, 420, 56, EchoUIView.HUD);
            }

            private void Settings()
            {
                var p = View(EchoUIView.Settings, "设置 / SETTINGS");
                var c = Card(p, "SettingsCard", 330, 160, 940, 620);
                Volume(c, "MasterVolume", "主音量 / Master", 42, .8f, b.RequestMasterVolume);
                Volume(c, "MusicVolume", "音乐 / Music", 134, .7f, b.RequestMusicVolume);
                Volume(c, "EffectsVolume", "音效 / Effects", 226, .8f, b.RequestEffectsVolume);
                Volume(c, "AmbienceVolume", "环境 / Ambience", 318, .7f, b.RequestAmbienceVolume);
                Label(c, "ShakeLabel", "震屏效果 / Screen shake", 40, 425, 600, 40, 23, Ivory);
                var toggleRect = Rect("ScreenShake", c, 774, 425, 40, 40);
                var toggle = toggleRect.gameObject.AddComponent<UnityEngine.UI.Toggle>();
                var bg = toggleRect.gameObject.AddComponent<UnityEngine.UI.Image>(); bg.color = Brass;
                var check = Box(toggleRect, "Checkmark", 8, 8, 24, 24, Cyan);
                toggle.targetGraphic = bg; toggle.graphic = check; toggle.SetIsOnWithoutNotify(true);
                UnityEventTools.AddPersistentListener(toggle.onValueChanged, b.RequestScreenShake);
                Action(c, "TextSpeedRequested", "文字速度 / Text speed · 普通（样例）", 40, 493, 860, 50, b.RequestTextSpeed);
                Label(c, "Scope", "滑杆仅发出设置请求；音频与偏好由 B 接入。", 40, 558, 860, 35, 19, Muted);
                var back = Button(p, "Back", "返回 / Back", 600, 791, 400, 52);
                UnityEventTools.AddPersistentListener(back.onClick, b.BackFromSettings);
            }

            private void Dialogue()
            {
                var p = View(EchoUIView.Dialogue, "对话 / DIALOGUE · 章节开场文案样例", false);
                var c = Card(p, "DialogueCard", 70, 560, 1460, 290);
                var portrait = Card(c, "EditablePortraitSlot", 24, 26, 154, 180);
                Label(portrait, "PortraitPlaceholder", "老周\n肖像待 D", 12, 52, 130, 76, 23, Brass, TextAlignmentOptions.Center);
                b.dialogueSpeakerLabel = Label(c, "Speaker", "老周 / Lao Zhou", 210, 25, 1140, 44, 27, Brass);
                b.dialogueBodyLabel = Label(c, "Body", "像那条停运的旧线，但还不能只凭回音下结论。\n去找找站牌和完整的录音吧。", 210, 78, 1190, 97, 28, Ivory);
                Label(c, "PlayerReply", "我下去找找站牌和完整录音。 / I'll look for the signs and a complete recording.", 210, 178, 1190, 43, 21, Cyan);
                Action(c, "ContinueDialogue", "F 继续 · 待接入", 820, 232, 250, 44, b.RequestDialogueContinue);
                Action(c, "SkipDialogue", "跳过 · 待接入", 1090, 232, 250, 44, b.RequestDialogueSkip);
                Label(p, "PreviewBadge", "对话样例 / UI PREVIEW · 不推进剧情", 70, 508, 1110, 37, 20, Muted);
                Nav(p, "PreviewBack", "返回 HUD 预览", 1230, 50, 300, 52, EchoUIView.HUD);
            }

            private void Map()
            {
                var p = View(EchoUIView.Map, "第一章探索地图 / CHAPTER 1 MAP · 三层 · 单主井");
                var topology = Rect("Topology", p, 65, 178, 1470, 560);
                var edges = Rect("Edges", topology, 0, 0, 1470, 560);
                var nodes = Rect("Nodes", topology, 0, 0, 1470, 560);
                // UI coordinates grow downwards. Middle and bottom landings share the top landing's x.
                var points = new Dictionary<string, Vector2> {
                    {"Outside", new Vector2(105,65)}, {"WalkIn", new Vector2(310,65)}, {"MapBoard", new Vector2(510,65)},
                    {"ShaftTop", new Vector2(730,65)}, {"ShaftMiddle", new Vector2(730,270)}, {"ShaftBottom", new Vector2(730,480)},
                    {"A", new Vector2(890,270)}, {"B", new Vector2(1060,270)}, {"C", new Vector2(1230,270)}, {"D", new Vector2(1400,270)},
                    {"E", new Vector2(1400,480)}, {"F", new Vector2(1160,480)}, {"G", new Vector2(920,480)} };
                string[] connections = { "Outside_WalkIn", "WalkIn_MapBoard", "MapBoard_ShaftTop", "ShaftTop_ShaftMiddle", "ShaftMiddle_A", "A_B", "B_C", "C_D", "D_E", "E_F", "F_G", "G_ShaftBottom", "ShaftBottom_ShaftMiddle" };
                foreach (string edge in connections)
                {
                    var ids = edge.Split('_');
                    // Test coordinates use anchoredPosition (up-positive), hence node y is negative below.
                    Line(edges, edge, points[ids[0]], points[ids[1]], edge.StartsWith("ShaftBottom") ? Cyan : Brass, 3);
                }
                var captions = new Dictionary<string, string> { {"Outside","洞外\n电台 / 工具棚"}, {"WalkIn","步行入洞"}, {"MapBoard","洞内地图板"}, {"ShaftTop","主井上站"}, {"ShaftMiddle","同井中站"}, {"ShaftBottom","同井下站"}, {"A","A\n入口安全段"}, {"B","B\n菌菇坡"}, {"C","C\n观察高廊"}, {"D","D\n下行洞道"}, {"E","E\n净化维护道"}, {"F","F\n候车厅"}, {"G","G\n旧站台"} };
                foreach (var entry in points)
                {
                    var node = Card(nodes, entry.Key, entry.Value.x - 66, entry.Value.y - 33, 132, 66);
                    // Centered anchor coordinates make deterministic topology inspectable.
                    node.pivot = new Vector2(.5f, .5f);
                    node.anchoredPosition = new Vector2(entry.Value.x, -entry.Value.y);
                    Label(node, "Caption", captions[entry.Key], 6, 4, 120, 58, 17, entry.Key.StartsWith("Shaft") ? Cyan : Ivory, TextAlignmentOptions.Center);
                }
                Label(p, "RowOne", "第一层 / SURFACE", 65, 153, 550, 30, 19, Brass);
                Label(p, "RowTwo", "第二层 / A → B → C → D", 65, 410, 560, 35, 21, Ivory);
                Label(p, "RowThree", "第三层 / G ← F ← E", 65, 617, 560, 35, 21, Ivory);
                Label(p, "Legend", "黄铜线：前进方向  ·  青线：同井返程向上\n连续步行；D→E 为下行洞道。路线图非比例，非传送菜单。", 65, 740, 1050, 66, 20, Muted);
                Nav(p, "PreviewBack", "返回 HUD 预览", 1210, 754, 310, 52, EchoUIView.HUD);
            }

            private void SoundLibrary()
            {
                var p = View(EchoUIView.SoundLibrary, "原声母带库 / MASTER RECORDINGS");
                Label(p, "Retention", "原始录音永久保留 / Masters retained · 内容与取得状态为静态示例", 65, 145, 1450, 40, 22, Cyan);
                string[] ids = { "S01", "S02", "S03", "S04" };
                string[] titles = { "灯尾蛾群鸣 / Glow moth chorus", "石背兽敲地 / Stoneback knock", "检修校准音 / Calibration tone", "汽笛片段 / Whistle fragment" };
                string[] details = { "第二层 · A 入口安全段 / 引导附近灯尾蛾照明", "原声引兽 / 2 号敲地波的能力来源", "第三层 · 维护设备校准与机关线索", "已暂存片段 / 于 G 补录完整汽笛" };
                for (int i = 0; i < 4; i++)
                {
                    var c = Card(p, ids[i], 65, 208 + i * 136, 1470, 120);
                    Label(c, "SoundId", ids[i], 24, 17, 80, 48, 30, Cyan);
                    Label(c, "Name", titles[i], 125, 12, 1000, 42, 26, Ivory);
                    Label(c, "Details", details[i], 125, 64, 1000, 38, 20, Muted);
                    var btn = Button(c, "Audition", "试听 · 待接入", 1180, 18, 260, 48);
                    UnityEventTools.AddStringPersistentListener(btn.onClick, b.RequestAudition, ids[i]);
                    Label(c, "MasterRetained", "母带保留", 1190, 76, 240, 29, 18, Brass, TextAlignmentOptions.Center);
                }
                Nav(p, "ToPlayback", "原声回放预览", 65, 772, 320, 48, EchoUIView.Playback);
                Nav(p, "PreviewBack", "返回 HUD 预览", 1210, 772, 320, 48, EchoUIView.HUD);
            }

            private void Playback()
            {
                var p = View(EchoUIView.Playback, "播放原声 / PLAYBACK SELECTION");
                var c = Card(p, "PlaybackCard", 330, 174, 940, 586);
                Label(c, "Instructions", "当前选择样例：S01 灯尾蛾群鸣\n环境播放 · 引导附近灯尾蛾照明", 38, 25, 864, 88, 25, Ivory);
                Wave(c, "Waveform", 42, 139, 850, 90);
                string[] ids = { "S01", "S02", "S03", "S04" };
                string[] names = { "S01  灯尾蛾群鸣 · 当前选择样例", "S02  石背兽敲地 · 原声引兽", "S03  检修校准音 · 未记录样例", "S04  汽笛片段 · 未记录样例" };
                for (int i = 0; i < 4; i++)
                {
                    var btn = Button(c, "Play" + ids[i], names[i] + (i < 2 ? " · 请求播放" : ""), 38, 254 + i * 66, 864, 52);
                    if (i < 2) UnityEventTools.AddStringPersistentListener(btn.onClick, b.RequestPlayback, ids[i]);
                    else btn.interactable = false;
                }
                Label(c, "NoAudio", "回放按钮只发出声音 ID；此预览不播放音频。", 38, 530, 864, 35, 20, Muted);
                Nav(p, "PreviewBack", "取消 / 返回 HUD 预览", 520, 779, 560, 52, EchoUIView.HUD);
            }

            private void Radio()
            {
                var p = View(EchoUIView.Radio, "矿井之声 / MINE RADIO · 单机节目样例");
                var soundscape = Card(p, "SinglePlayerSoundscape", 65, 160, 960, 486);
                Label(soundscape, "Recording", "S01  灯尾蛾群鸣", 26, 22, 900, 52, 32, Ivory);
                Label(soundscape, "SceneCopy", "来自矿井深处的夜，\n成千上万的灯尾蛾在空间中鸣唱。", 26, 86, 900, 91, 26, Muted);
                Wave(soundscape, "SoundscapeWave", 30, 214, 900, 130);
                Label(soundscape, "StaticClockNote", "静态快照 / STATIC SNAPSHOT", 26, 371, 600, 30, 18, Brass);
                b.radioClockLabel = Label(soundscape, "Clock", "00:20 / 01:00", 680, 368, 250, 42, 25, Cyan, TextAlignmentOptions.Right);
                Meter(soundscape, "Timeline", 30, 430, 900, 10, 20f / 60f);
                var highlight = Box(soundscape, "Highlight15To25", 30 + 900 * 15f / 60f, 424, 900 * 10f / 60f, 22, new Color(Cyan.r, Cyan.g, Cyan.b, .32f));
                highlight.raycastTarget = false;
                var audience = Card(p, "AudienceAndCopies", 1050, 160, 485, 486);
                Label(audience, "Heading", "听众与副本 / LISTENERS", 24, 22, 438, 42, 25, Brass);
                Label(audience, "Audience", "听众 3 人（快照）\n\n“像星星在矿洞里飞。”\n\n已购声音副本 3 份\n本场销售 12 元（中途样例）", 24, 92, 438, 240, 25, Ivory);
                Label(audience, "Retention", "听众购买数字副本。\n原声母带永久保留。", 24, 354, 438, 87, 23, Cyan);
                Action(p, "StartBroadcast", "播出节目 · 待接入", 65, 675, 450, 60, b.RequestBroadcast);
                Action(p, "HighlightRequested", "精彩片段 · 待接入", 550, 675, 475, 60, b.RequestHighlight);
                Label(p, "HighlightRule", "15–25 秒 · 每场仅一次 / One highlight per 60-second show · 此处无计时器", 65, 752, 1050, 44, 20, Brass);
                Nav(p, "ToSettlementPreview", "结算样例 →", 1090, 699, 390, 60, EchoUIView.Settlement);
            }

            private void Shop()
            {
                var p = View(EchoUIView.Shop, "小林的电池卡 / OPTIONAL UPGRADE SHOP");
                var c = Card(p, "BatteryUpgrade", 100, 177, 860, 570);
                Label(c, "Heading", "电池升级 / Battery capacity", 38, 26, 784, 60, 38, Ivory);
                Label(c, "Capacity", "100   →   120", 38, 118, 784, 86, 58, Cyan, TextAlignmentOptions.Center);
                Label(c, "Price", "价格 / Price                    18 元", 38, 234, 784, 50, 30, Brass);
                b.balanceLabel = Label(c, "Balance", "当前余额 / Balance           22 元\n购买后 / After purchase        4 元", 38, 308, 784, 105, 29, Ivory);
                Label(c, "Optional", "可选升级；不购买也可继续主线。", 38, 469, 784, 54, 23, Muted);
                var other = Card(p, "UnavailableOption", 1010, 177, 490, 260);
                Label(other, "Label", "其他升级 / OTHER OPTIONS\n\nA L2 · 详情待定\n暂未开放", 26, 22, 438, 208, 26, Muted);
                var insufficient = Card(p, "InsufficientFundsSnapshot", 1010, 470, 490, 277);
                Label(insufficient, "Label", "余额不足状态示例\n\n余额 14 元 < 价格 18 元\n无法购买", 26, 22, 438, 158, 24, Muted);
                var disabled = Button(insufficient, "UnavailablePurchase", "余额不足 / Unavailable", 26, 201, 438, 48); disabled.interactable = false;
                Action(p, "PurchaseBattery", "购买升级 · 待接入", 100, 766, 540, 52, b.RequestPurchaseBattery);
                Nav(p, "PreviewBack", "取消 / 返回 HUD 预览", 1010, 766, 490, 52, EchoUIView.HUD);
            }

            private void Settlement()
            {
                var p = View(EchoUIView.Settlement, "节目完成 / BROADCAST SETTLEMENT");
                var c = Card(p, "SettlementCard", 330, 178, 940, 545);
                Label(c, "Recording", "S01  灯尾蛾群鸣", 40, 26, 860, 60, 38, Ivory);
                Wave(c, "Waveform", 40, 106, 860, 72);
                Label(c, "Copies", "声音副本  6 份 × 4 元              24 元", 40, 212, 860, 50, 30, Ivory);
                Label(c, "Electricity", "电费 / Electricity                       −2 元", 40, 279, 860, 50, 30, Brass);
                Box(c, "Rule", 40, 348, 860, 1, Brass);
                Label(c, "Net", "净收入 / Net income                  22 元", 40, 368, 860, 60, 36, Cyan);
                Label(c, "Retention", "母带已保留 / Masters retained\n以上为明确的结算样例，不代表实时交易结果。", 40, 451, 860, 67, 22, Muted);
                Action(p, "Acknowledge", "确认 · 待接入", 330, 751, 440, 58, b.RequestSettlementAcknowledge);
                Nav(p, "ToShopPreview", "商店样例 →", 830, 751, 440, 58, EchoUIView.Shop);
            }

            private void Fail()
            {
                var p = View(EchoUIView.Fail, "暂时倒下 / REST & RECOVER");
                b.failPanel = p.gameObject;
                var c = Card(p, "FailCard", 330, 200, 940, 520);
                Label(c, "Title", "暂时倒下", 45, 32, 850, 80, 54, Ivory, TextAlignmentOptions.Center);
                Label(c, "Explanation", "探险中体力不支，暂时倒下。\n已保存的录音与永久进度保留。\n未完成的遭遇将恢复初始状态。", 65, 154, 810, 150, 30, Ivory, TextAlignmentOptions.Center);
                Action(c, "RetryFromRest", "从最近休息点继续 · 待接入", 90, 350, 760, 58, b.RequestRetryFromRest);
                Action(c, "MenuRequested", "返回主菜单 · 待接入", 90, 427, 760, 58, b.RequestReturnToMenu);
                Nav(p, "PreviewBack", "返回 HUD 预览", 570, 747, 460, 56, EchoUIView.HUD);
            }

            private void End()
            {
                var p = View(EchoUIView.End, "第一章调查完成 / CHAPTER ONE COMPLETE");
                var c = Card(p, "EndCard", 240, 180, 1120, 540);
                Label(c, "Title", "让矿井的声音，被更多人听见。", 50, 32, 1020, 65, 42, Ivory, TextAlignmentOptions.Center);
                Label(c, "Explanation", "你收集了关键线索，并在电台播出了节目。\n这是章末完成状态的静态示例。", 50, 126, 1020, 92, 28, Muted, TextAlignmentOptions.Center);
                Label(c, "Checklist", "[已完成样例] 完整汽笛       [已完成样例] 线路图\n[已完成样例] 返程通路       [已完成样例] 首次节目", 75, 255, 970, 98, 27, Cyan, TextAlignmentOptions.Center);
                Nav(c, "ContinueOrganizingPreview", "继续整理 / 返回 HUD 预览", 70, 388, 980, 56, EchoUIView.HUD);
                Action(c, "SaveAndReturn", "保存并返回主菜单 · 待接入", 70, 465, 980, 56, b.RequestSaveAndReturn);
            }

            private RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(width, height);
                return rt;
            }

            private static void Stretch(RectTransform rt)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, .5f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }

            private UnityEngine.UI.Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
            {
                var rt = Rect(name, parent, x, y, w, h);
                var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
                return image;
            }

            private void Frame(Transform parent, float x, float y, float w, float h)
            {
                Box(parent, "BorderTop", x, y, w, 1, Brass); Box(parent, "BorderBottom", x, y + h - 1, w, 1, Brass);
                Box(parent, "BorderLeft", x, y, 1, h, Brass); Box(parent, "BorderRight", x + w - 1, y, 1, h, Brass);
                foreach (var v in new[] { new Vector2(x,y), new Vector2(x+w-5,y), new Vector2(x,y+h-5), new Vector2(x+w-5,y+h-5) })
                    Box(parent, "CornerRivet", v.x, v.y, 5, 5, Brass);
            }

            private RectTransform Card(Transform parent, string name, float x, float y, float w, float h)
            {
                var rt = Rect(name, parent, x, y, w, h);
                var bg = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); bg.color = Panel; bg.raycastTarget = false;
                Frame(rt, 0, 0, w, h); return rt;
            }

            private TextMeshProUGUI Label(Transform parent, string name, string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
            {
                var rt = Rect(name, parent, x, y, w, h);
                var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
                label.font = font; label.fontSharedMaterial = font.material; label.text = text;
                label.fontSize = size; label.color = color; label.alignment = alignment;
                label.raycastTarget = false; label.richText = false;
                label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Overflow;
                label.margin = Vector4.zero;
                return label;
            }

            private UnityEngine.UI.Button Button(Transform parent, string name, string caption, float x, float y, float w, float h)
            {
                // Outer Image is a one-pixel border and tint target; inner fill stays navy.
                var border = Box(parent, name, x, y, w, h, Color.white); border.raycastTarget = true;
                var btn = border.gameObject.AddComponent<UnityEngine.UI.Button>(); btn.targetGraphic = border;
                var colors = btn.colors;
                colors.normalColor = Brass; colors.highlightedColor = Cyan; colors.selectedColor = Cyan;
                colors.pressedColor = Ivory; colors.disabledColor = Hex("46515d"); colors.fadeDuration = .08f; btn.colors = colors;
                Box(border.transform, "Fill", 1, 1, w - 2, h - 2, Navy);
                Label(border.transform, "Label", caption, 10, 4, w - 20, h - 8, h <= 48 ? 18 : 23, Ivory, TextAlignmentOptions.Center);
                var layout = border.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.preferredWidth = w; layout.preferredHeight = h; layout.minWidth = w; layout.minHeight = h;
                var view = views.LastOrDefault(v => border.transform.IsChildOf(v.panel.transform));
                if (view != null && view.firstSelected == null) view.firstSelected = btn;
                return btn;
            }

            private void Action(Transform parent, string name, string caption, float x, float y, float w, float h, UnityAction callback)
            {
                var button = Button(parent, name, caption, x, y, w, h);
                UnityEventTools.AddPersistentListener(button.onClick, callback);
            }

            private void Nav(Transform parent, string name, string caption, float x, float y, float w, float h, EchoUIView target)
            {
                var button = Button(parent, name, caption, x, y, w, h);
                UnityEventTools.AddIntPersistentListener(button.onClick, b.ShowView, (int)target);
            }

            private void SettingsButton(Transform parent, float x, float y, float width)
            {
                Action(parent, "OpenSettings", "设置 / Settings", x, y, width, 58, b.OpenSettings);
            }

            private void Horizontal(RectTransform rt, float spacing)
            {
                var layout = rt.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.spacing = spacing; layout.childControlWidth = true; layout.childControlHeight = true;
                layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            }

            private void Meter(Transform parent, string name, float x, float y, float w, float h, float fraction)
            {
                var bg = Box(parent, name, x, y, w, h, Hex("253b50"));
                Box(bg.transform, "Fill", 0, 0, w * fraction, h, Cyan);
            }

            private void Volume(Transform parent, string name, string caption, float y, float value, UnityAction<float> callback)
            {
                Label(parent, name + "Label", caption, 40, y, 390, 42, 25, Ivory);
                var rt = Rect(name, parent, 430, y + 7, 380, 32);
                var track = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); track.color = Hex("253b50"); track.raycastTarget = true;
                var slider = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();
                slider.minValue = 0; slider.maxValue = 1;
                var fillArea = Rect("FillArea", rt, 12, 10, 356, 12);
                var fill = Box(fillArea, "Fill", 0, 0, 356, 12, Cyan).rectTransform;
                var handleArea = Rect("HandleArea", rt, 12, 0, 356, 32);
                var handle = Box(handleArea, "Handle", 0, 0, 18, 32, Ivory);
                slider.fillRect = fill; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
                slider.SetValueWithoutNotify(value);
                UnityEventTools.AddPersistentListener(slider.onValueChanged, callback);
                Label(parent, name + "ExampleValue", Mathf.RoundToInt(value * 100) + "%\n样例", 831, y - 3, 78, 53, 17, Cyan, TextAlignmentOptions.Center);
            }

            private void Wave(Transform parent, string name, float x, float y, float w, float h)
            {
                var rt = Rect(name, parent, x, y, w, h);
                // Fixed deterministic decorative samples, not measured sound or runtime visualization.
                int[] samples = { 2, 5, 9, 14, 8, 4, 12, 18, 10, 6, 3, 11, 16, 7, 4, 9, 13, 5, 2, 8, 12, 6, 3, 7, 10, 4, 2, 6, 8, 3, 5, 2 };
                float step = w / samples.Length;
                for (int i = 0; i < samples.Length; i++)
                {
                    float height = h * samples[i] / 18f;
                    Box(rt, "Sample" + i.ToString("00"), step * i, (h - height) * .5f, Mathf.Max(2, step * .36f), height, Cyan);
                }
            }

            private void Line(Transform parent, string name, Vector2 from, Vector2 to, Color color, float thickness)
            {
                var delta = new Vector2(to.x - from.x, -(to.y - from.y));
                var image = Box(parent, name, from.x, from.y, delta.magnitude, thickness, color);
                var rt = image.rectTransform; rt.pivot = new Vector2(0, .5f);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                // Two simple editable ticks form a chevron at 60% of each connection.
                Vector2 direction = delta.normalized;
                Vector2 tip = new Vector2(from.x, -from.y) + delta * .6f;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                ArrowTick(parent, name + "ArrowL", tip - direction * 12 + normal * 7, tip, color);
                ArrowTick(parent, name + "ArrowR", tip - direction * 12 - normal * 7, tip, color);
            }

            private void ArrowTick(Transform parent, string name, Vector2 from, Vector2 to, Color color)
            {
                // Decorative ticks live outside Edges so edge-count and connectivity remain unambiguous.
                var topology = parent.parent;
                var arrowRoot = topology.Find("DirectionChevrons") as RectTransform;
                if (arrowRoot == null) arrowRoot = Rect("DirectionChevrons", topology, 0, 0, 1470, 560);
                Vector2 d = to - from;
                var line = Box(arrowRoot, name, from.x, -from.y, d.magnitude, 3, color).rectTransform;
                line.pivot = new Vector2(0, .5f); line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }
    }
}
#endif
