#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using EchoCaves.UI;

namespace EchoCaves.Editor
{
    /// <summary>Contract assertions written before the builder. Invoke on a fresh root before integration.</summary>
    public static class EchoUIContractTests
    {
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Echo UI contract: " + message);
        }

        /// <summary>Read-only checks main can invoke before Build; creates no native hierarchy or assets.</summary>
        public static string Preflight()
        {
            Require(AssetDatabase.LoadAssetAtPath<Font>(EchoUIBuilder.FontSourcePath) != null, "Source font has not been imported.");
            Require(Shader.Find("TextMeshPro/Mobile/Distance Field") != null, "TMP shader missing.");
            var build = typeof(EchoUIBuilder).GetMethod("Build", new[] { typeof(Transform) });
            Require(build != null && build.IsPublic && build.IsStatic && build.ReturnType == typeof(GameObject)
                && build.GetParameters()[0].IsOptional, "Build(Transform parent=null) API changed.");
            Require(typeof(EchoUIBuilder).GetMethod("Validate", Type.EmptyTypes)?.ReturnType == typeof(string), "Validate API changed.");
            Require(typeof(EchoUIBuilder).GetMethod("CaptureScreens", new[] { typeof(string) })?.ReturnType == typeof(string[]),
                "CaptureScreens(string folder) must return thirteen native PNG paths.");
            Require(!typeof(EchoUIBindings).GetMethods().Any(m => m.Name == "Update" || m.Name == "FixedUpdate"), "Presentation bindings must not run gameplay ticks.");
            return "PASS: read-only preflight; build signature, imported font and TMP shader available.";
        }

        public static string AssertRoot(GameObject root)
        {
            Require(root != null, "Build must return a root Canvas.");
            var canvas = root.GetComponent<Canvas>();
            Require(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Overlay Canvas required.");
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            Require(scaler != null && scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize
                && scaler.referenceResolution == new Vector2(1600, 900), "1600x900 scale contract.");
            Require(root.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null, "Raycaster missing.");
            var bindings = root.GetComponent<EchoUIBindings>();
            Require(bindings != null && bindings.Views.Length == 13, "All thirteen presentation views must be serialized.");
            foreach (EchoUIView id in Enum.GetValues(typeof(EchoUIView)))
            {
                var view = bindings.Views.SingleOrDefault(v => v.id == id);
                Require(view != null && view.panel != null && view.panel.transform.parent == root.transform, "Missing root child: " + id);
                Require(view.panel.activeSelf == (id == EchoUIView.HUD), "HUD alone must be active by default: " + id);
            }
            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            Require(texts.Length > 70 && texts.All(t => t.font != null && EditorUtility.IsPersistent(t.font)), "Persistent TMP font required on editable labels.");
            Require(root.GetComponentInChildren<UnityEngine.UI.Text>(true) == null, "Use TMP throughout.");
            Require(texts.Any(t => t.text.Contains("预览")), "Honest preview labels must remain authored, including hidden examples.");
            AssertCleanHUD(root);
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Require(button.targetGraphic != null && button.targetGraphic.raycastTarget, "Button has no raycast target: " + button.name);
                Require(!button.interactable || button.onClick.GetPersistentEventCount() > 0, "Unwired button: " + button.name);
            }
            var boss = root.transform.Find("HUD/BossStagger/Markers");
            Require(boss != null && boss.childCount == 3, "Boss must have exactly three markers.");
            Require(boss.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Length == 0, "Boss cannot be an HP bar.");
            foreach (Transform marker in boss)
            {
                var rect = marker as RectTransform;
                Require(rect != null && rect.sizeDelta.x == rect.sizeDelta.y
                    && marker.GetComponent<UnityEngine.UI.Image>().color == Color.white, "Boss markers must be white squares.");
            }
            var edges = root.transform.Find("Map/Topology/Edges");
            var nodes = root.transform.Find("Map/Topology/Nodes");
            string[] expectedEdges = { "Outside_WalkIn", "WalkIn_MapBoard", "MapBoard_ShaftTop", "ShaftTop_ShaftMiddle", "ShaftMiddle_A", "A_B", "B_C", "C_D", "D_E", "E_F", "F_G", "G_ShaftBottom", "ShaftBottom_ShaftMiddle" };
            Require(edges != null && edges.childCount == expectedEdges.Length, "Map edge count/topology changed.");
            foreach (string edge in expectedEdges) Require(edges.Find(edge) != null, "Map edge missing: " + edge);
            Require(nodes != null && nodes.childCount == 13, "Map needs explicit entrance, shaft landings and A-G nodes.");
            Vector2 Pos(string name) => ((RectTransform)nodes.Find(name)).anchoredPosition;
            Require(Pos("Outside").x < Pos("WalkIn").x && Pos("WalkIn").x < Pos("MapBoard").x
                && Pos("MapBoard").x < Pos("ShaftTop").x, "Walk inside before reaching shaft.");
            Require(Pos("ShaftTop").x == Pos("ShaftMiddle").x && Pos("ShaftMiddle").x == Pos("ShaftBottom").x,
                "All three landings must belong to the same left shaft.");
            Require(Pos("ShaftMiddle").x < Pos("A").x && Pos("A").x < Pos("B").x && Pos("B").x < Pos("C").x
                && Pos("C").x < Pos("D").x && Pos("D").y > Pos("E").y && Pos("D").x == Pos("E").x,
                "Second row must run A-B-C-D right, then D down to E.");
            Require(Pos("E").x > Pos("F").x && Pos("F").x > Pos("G").x && Pos("G").x > Pos("ShaftBottom").x,
                "Third row must return E-F-G left to the same shaft.");
            Require(bindings.radioDurationSeconds == 60 && bindings.highlightStartSeconds == 15 && bindings.highlightEndSeconds == 25,
                "Single radio 60 seconds, one 15-25 second sample highlight.");
            Require(bindings.sampleCopies == 6 && bindings.sampleUnitPrice == 4 && bindings.sampleElectricity == 2
                && bindings.sampleNetIncome == 22 && bindings.sampleUpgradePrice == 18 && bindings.sampleAfterUpgrade == 4,
                "Explicit settlement/upgrade samples required.");
            Require(bindings.mastersRetained, "Master recordings are retained.");
            var menu = root.transform.Find("MainMenu");
            var menuBackground = menu.GetComponent<UnityEngine.UI.Image>();
            Require(menuBackground == null || menuBackground.color.a == 0, "Main menu must reveal the chapter camera.");
            var backing = menu.Find("LeftBacking").GetComponent<UnityEngine.UI.Image>();
            Require(backing.color.a > 0 && backing.color.a < 1 && backing.rectTransform.sizeDelta.x == 720,
                "Menu needs translucent left 45% backing.");
            Require(menu.Find("EditableSoundscape") == null && menu.Find("Pitch") == null, "Reference menu has no waveform/tagline card.");
            Require(((RectTransform)menu.Find("Wordmark")).anchoredPosition == new Vector2(140, -190), "Reference title anchor changed.");
            foreach (string name in new[] { "ContinueGame", "NewGame", "OpenSettings", "Quit" })
            {
                var buttonRect = (RectTransform)menu.Find(name);
                Require(buttonRect != null && buttonRect.anchoredPosition.x == 160 && buttonRect.sizeDelta.x == 450,
                    "Reference left menu button layout: " + name);
            }
#if ENABLE_INPUT_SYSTEM
            var module = root.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(true);
            if (module != null)
            {
                Require(module.actionsAsset != null && EditorUtility.IsPersistent(module.actionsAsset), "Prefab input asset must be persistent.");
                AssertInputReferences(module);
            }
#endif
            return "PASS: 13 views, serialized buttons/TMP, default HUD, 13 map edges, three white boss markers, explicit radio/settlement samples.";
        }

        public static string AssertCleanHUD(GameObject root)
        {
            var hud = root.transform.Find("HUD");
            Require(hud != null && hud.gameObject.activeSelf, "Default HUD view must remain active.");
            foreach (string name in new[] { "BossStagger", "RecordingSnapshot", "FeedbackSnapshot", "PreviewNavigation" })
            {
                var state = hud.Find(name);
                Require(state != null && !state.gameObject.activeSelf, "Clean default HUD must hide " + name + " while preserving it for B.");
            }
            Require(hud.Find("Vitals").gameObject.activeSelf && hud.Find("Objective").gameObject.activeSelf,
                "Clean default HUD must keep Vitals and Objective visible.");
            return "PASS: clean HUD, four state/demo groups hidden; Vitals and Objective visible.";
        }

#if ENABLE_INPUT_SYSTEM
        public static string AssertInputReferences(UnityEngine.InputSystem.UI.InputSystemUIInputModule module)
        {
            var expected = new[] {
                ("m_PointAction", "Point", module.point), ("m_LeftClickAction", "Click", module.leftClick),
                ("m_ScrollWheelAction", "Scroll", module.scrollWheel), ("m_MoveAction", "Navigate", module.move),
                ("m_SubmitAction", "Submit", module.submit), ("m_CancelAction", "Cancel", module.cancel) };
            var serialized = new SerializedObject(module);
            string path = AssetDatabase.GetAssetPath(module.actionsAsset);
            foreach (var (field, action, reference) in expected)
            {
                Require(reference != null && EditorUtility.IsPersistent(reference) && AssetDatabase.IsSubAsset(reference)
                    && AssetDatabase.GetAssetPath(reference) == path && reference.asset == module.actionsAsset
                    && reference.action != null && reference.action.name == action,
                    "Prefab input references must survive serialization: " + field + " / " + action
                    + "; reference path=" + AssetDatabase.GetAssetPath(reference));
                Require(serialized.FindProperty(field).objectReferenceValue == reference, "Serialized input slot differs: " + field);
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out string guid, out long localId)
                    && !string.IsNullOrEmpty(guid) && localId != 0, "No persistent file ID for input: " + action);
            }
            return "PASS: six UI input slots point to persistent, file-ID-backed subassets.";
        }

        // Regression: reconfiguration must replace same-action transient references, preserve IDs and be repeatable.
        public static string AssertInputRebinding(GameObject root)
        {
            var module = root.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(true);
            Require(module != null, "Rebinding test needs the isolated root's input module.");
            var persistent = module.point;
            var temporary = UnityEngine.InputSystem.InputActionReference.Create(persistent.action);
            try
            {
                module.point = null;
                module.point = temporary;
                Require(!EditorUtility.IsPersistent(module.point), "Regression setup must use a transient same-action reference.");
                EchoUIBuilder.RepairPreviewInput(root);
                AssertInputReferences(module);
                Require(module.point == persistent, "Repair must reuse existing reference identity.");
                EchoUIBuilder.RepairPreviewInput(root);
                Require(module.point == persistent, "Second repair must be idempotent.");
                return "PASS: transient same-action reference replaced; persistent reference reused on repeated repair.";
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }
#endif

        public static string AssertNavigation(GameObject root)
        {
            var b = root.GetComponent<EchoUIBindings>();
            foreach (EchoUIView id in Enum.GetValues(typeof(EchoUIView)))
            {
                b.ShowView((int)id);
                Require(b.Views.Count(v => v.panel.activeSelf) == 1 && b.Views.Single(v => v.id == id).panel.activeSelf,
                    "Switch must show exactly one view: " + id);
            }
            AssertRepeatedSettingsReturn(root);
            int requested = 0;
            UnityEngine.Events.UnityAction listener = () => requested++;
            b.onPurchaseBatteryRequested.AddListener(listener);
            b.ShowView((int)EchoUIView.Shop);
            var before = b.sampleAfterUpgrade;
            var purchase = root.transform.Find("Shop/PurchaseBattery").GetComponent<UnityEngine.UI.Button>().onClick;
            Require(purchase.GetPersistentEventCount() == 1 && purchase.GetPersistentTarget(0) == b
                && purchase.GetPersistentMethodName(0) == "RequestPurchaseBattery", "Purchase button serialized target is incorrect.");
            var originalState = purchase.GetPersistentListenerState(0);
            try
            {
                // RuntimeOnly callbacks are intentionally skipped by Unity in Edit mode.
                // Enable this one callback on the disposable test instance, then restore it.
                purchase.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
                purchase.Invoke();
                Require(requested == 1 && b.CurrentView == EchoUIView.Shop && b.sampleAfterUpgrade == before,
                    "Purchase button must emit intent only; no economy or fake completion.");
            }
            finally
            {
                purchase.SetPersistentListenerState(0, originalState);
                b.onPurchaseBatteryRequested.RemoveListener(listener);
                b.ShowView((int)EchoUIView.HUD);
            }
            return "PASS: all view switches, repeated Settings open + Back preserves MainMenu/Pause callers, purchase intent without economy mutation.";
        }

        private static void AssertRepeatedSettingsReturn(GameObject root)
        {
            var bindings = root.GetComponent<EchoUIBindings>();
            foreach (var caller in new[] { EchoUIView.MainMenu, EchoUIView.Pause })
            {
                bindings.ShowView((int)caller);
                bindings.OpenSettings();
                bindings.OpenSettings();
                bindings.OpenSettings();
                Require(bindings.CurrentView == EchoUIView.Settings && bindings.Views.Count(v => v.panel.activeSelf) == 1,
                    "Repeated open must keep Settings as the only active view.");
                bindings.BackFromSettings();
                Require(bindings.CurrentView == caller && bindings.Views.Single(v => v.id == caller).panel.activeSelf,
                    "Repeated OpenSettings + Back must preserve original caller " + caller + "; actual=" + bindings.CurrentView);
            }
            bindings.ShowView((int)EchoUIView.HUD);
        }

        /// <summary>Native RED/GREEN entry; never uses or saves shared scene UI.</summary>
        public static string RepeatedSettingsReturnRegression()
        {
            return WithDisposableRoot(root => {
                AssertRepeatedSettingsReturn(root);
                return "PASS: native repeatOpen(3)+Back returns to MainMenu, then Pause; HUD restored.";
            });
        }

        public static string CleanHUDRegression()
        {
            return WithDisposableRoot(root => {
                AssertCleanHUD(root);
                int expectedLabelCount = root.GetComponentsInChildren<TextMeshProUGUI>(true).Length;
                string audit = EchoUIBuilder.ValidateTextLayout(root);
                Require(audit.Contains(expectedLabelCount + " labels measured"), "Layout audit must cover hidden HUD example labels too.");
                AssertCleanHUD(root);
                return audit + "\nPASS: clean HUD before and after audit; all hidden example labels covered.";
            });
        }

        private static string WithDisposableRoot(Func<GameObject, string> assertion)
        {
            var shared = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = shared.GetRootGameObjects().Select(g => g.GetInstanceID()).OrderBy(id => id).ToArray();
            bool dirty = shared.isDirty;
            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            GameObject host = null;
            try
            {
                host = new GameObject("EchoUIRegression_Disposable_NotShared");
                host.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host, preview);
                var root = EchoUIBuilder.Build(host.transform);
                return assertion(root);
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
                Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene() == shared && shared.isDirty == dirty
                    && shared.GetRootGameObjects().Select(g => g.GetInstanceID()).OrderBy(id => id).SequenceEqual(roots),
                    "Settings regression must preserve shared scene roots/dirty state.");
            }
        }
    }
}
#endif
