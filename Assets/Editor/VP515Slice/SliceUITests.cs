using System;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Parent-owned Editor invocation: Debug.Log(SliceUITests.Run()).
/// Reflection lets the RED suite compile before the production UI exists.
/// Creates temporary objects only; never saves or changes a scene.</summary>
public static class SliceUITests
{
    public static string Run()
    {
        var report = new StringBuilder();
        int passed = 0, failed = 0;
        var uiType = FindType("VP515.Slice.SliceUI");
        if (uiType == null) return "FAIL MissingProductionUI: VP515.Slice.SliceUI is absent (expected RED).";
        var viewType = FindType("VP515.Slice.SliceView");
        var hudType = FindType("VP515.Slice.SliceHudData");
        var font = AssetDatabase.FindAssets("t:TMP_FontAsset")
            .Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(f => f != null);
        if (font == null) return "FAIL FontFixture: parent must import/inject a TMP font before running UI tests.";
        var oldSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var host = new GameObject("SliceUITestFixture") { hideFlags = HideFlags.HideAndDontSave };
        var ui = host.AddComponent(uiType);
        void Call(string method, params object[] args) => uiType.GetMethod(method).Invoke(ui, args);
        void Show(string view) => Call("Show", Enum.Parse(viewType, view));
        string Current() => uiType.GetProperty("CurrentView").GetValue(ui).ToString();
        Transform Node(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        UnityEngine.UI.Button Button(string name) => Node(name).GetComponent<UnityEngine.UI.Button>();
        void Click(string name)
        {
            var b = Button(name);
            Require(b.gameObject.activeInHierarchy && b.IsInteractable(), name + " is not available");
            b.onClick.Invoke();
        }
        object Hud(float health, int dew, bool butterfly = false)
        {
            var data = Activator.CreateInstance(hudType);
            hudType.GetField("Health").SetValue(data, health);
            hudType.GetField("Energy").SetValue(data, 52f);
            hudType.GetField("DewCount").SetValue(data, dew);
            hudType.GetField("Butterfly").SetValue(data, butterfly);
            hudType.GetField("Objective").SetValue(data, "Activate the sigil");
            hudType.GetField("Prompt").SetValue(data, "E  Inspect");
            return data;
        }
        void Test(string name, Action body)
        {
            try { body(); passed++; report.AppendLine("PASS " + name); }
            catch (Exception ex) { failed++; report.AppendLine("FAIL " + name + ": " + (ex.InnerException ?? ex).Message); }
        }
        try
        {
            // Bug caught: Build omits raycaster/scaler or duplicates Canvas on a second call.
            Test("BuildCreatesOneResponsiveInteractiveCanvas", () =>
            {
                Call("Build", font); Call("Build", font);
                var canvases = host.GetComponentsInChildren<Canvas>(true);
                Require(canvases.Length == 1, "Build duplicated or omitted Canvas");
                Require(canvases[0].GetComponent<UnityEngine.UI.GraphicRaycaster>() != null, "No raycaster");
                var scaler = canvases[0].GetComponent<UnityEngine.UI.CanvasScaler>();
                Require(scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                    scaler.referenceResolution == new Vector2(1920, 1080), "Wrong responsive scale");
                Require(host.GetComponentsInChildren<TMP_Text>(true).All(t => t.font == font), "Font was not injected");
                Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == Math.Max(1, oldSystems.Length), "Duplicated EventSystem");
            });
            // Bug caught: Start button missing its callback or emitting twice.
            Test("StartClickEmitsOneRequest", () =>
            {
                int count = 0; uiType.GetField("StartRequested").SetValue(ui, (Action)(() => count++));
                Show("Menu"); Click("StartButton"); Require(count == 1, "Expected one start callback");
            });
            // Bug caught: Show omits ViewChanged or reports the page before the transition.
            Test("ShowPublishesCurrentViewIncludingRepeatedSynchronization", () =>
            {
                int count = 0; string payload = "";
                Action<object> observe = value => { count++; payload = value.ToString(); Require(Current() == payload, "View event precedes state"); };
                var callback = typeof(SliceUITests).GetMethod(nameof(ViewObserver), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(viewType).Invoke(null, new object[] { observe });
                uiType.GetField("ViewChanged").SetValue(ui, callback);
                Show("Playing"); Show("Paused"); Show("Paused");
                Require(count == 3 && payload == "Paused", "Missing or wrong view notification");
                uiType.GetField("ViewChanged").SetValue(ui, null);
            });
            // Bug caught: hover/keyboard focus lacks a visible marker/context, or wrong request is wired.
            Test("PointerAndKeyboardShareFeedbackAndPauseRequests", () =>
            {
                int resumed = 0, quit = 0;
                uiType.GetField("ResumeRequested").SetValue(ui, (Action)(() => resumed++));
                uiType.GetField("QuitRequested").SetValue(ui, (Action)(() => quit++));
                Show("Menu"); Click("QuitButton"); Require(quit == 1, "Quit request missing");
                var target = Button("MenuSettingsButton").gameObject;
                EventSystem.current.SetSelectedGameObject(target);
                Require(target.transform.Find("SelectionAccent").gameObject.activeSelf && Node("Tooltip").gameObject.activeInHierarchy, "Keyboard selection has no feedback");
                EventSystem.current.SetSelectedGameObject(null);
                ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
                Require(target.transform.Find("SelectionAccent").gameObject.activeSelf && Node("Tooltip").gameObject.activeInHierarchy, "Hover has no feedback");
                ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
                Show("Paused"); Click("ResumeButton"); Require(resumed == 1, "Resume callback missing");
            });
            // Bug caught: Show hides HUD during pause or leaves an old modal visible.
            Test("PauseKeepsHudVisibleAndSwitchesModal", () =>
            {
                Show("Paused"); Require(Node("HUD").gameObject.activeInHierarchy, "Paused HUD missing");
                Require(Node("PausedPanel").gameObject.activeInHierarchy, "Pause panel missing");
                Show("Playing"); Require(!Node("PausedPanel").gameObject.activeInHierarchy, "Old modal remains visible");
            });
            // Bug caught: empty/full-health/butterfly dew appears actionable or dispatch bypasses its guard.
            Test("UnavailableDewIsDisabledWithReason", () =>
            {
                int count = 0; uiType.GetField("DewRequested").SetValue(ui, (Action)(() => count++));
                Show("Inventory");
                foreach (var data in new[] { Hud(40, 0), Hud(100, 1), Hud(40, 1, true), Hud(0, 1) })
                {
                    Call("UpdateHUD", data);
                    Require(!Button("UseDewButton").interactable, "Unavailable dew remains enabled");
                    Require(!string.IsNullOrWhiteSpace(Node("DewStatus").GetComponent<TMP_Text>().text), "No unavailable reason");
                    Button("UseDewButton").onClick.Invoke(); Require(count == 0, "Guard bypassed by callback");
                }
                Call("UpdateHUD", Hud(40, 1)); Click("UseDewButton"); Require(count == 1, "Usable dew callback missing");
            });
            // Bug caught: settings/controls Back unpauses gameplay or forgets nested context.
            Test("SubpanelBackRestoresOriginWithoutResuming", () =>
            {
                int resumed = 0; uiType.GetField("ResumeRequested").SetValue(ui, (Action)(() => resumed++));
                Show("Menu"); Click("MenuControlsButton"); Click("ControlsBackButton"); Require(Current() == "Menu", "Controls lost menu origin");
                Show("Paused"); Click("PauseSettingsButton"); Click("SettingsBackButton"); Require(Current() == "Paused", "Settings resumed gameplay");
                Show("Inventory"); Show("Controls"); Show("Settings"); Click("SettingsBackButton");
                Require(Current() == "Controls", "Nested context lost"); Click("ControlsBackButton");
                Require(Current() == "Inventory" && resumed == 0, "Back resumed or lost inventory");
                Show("Playing"); Show("Settings"); Click("SettingsBackButton"); Require(Current() == "Paused", "Direct gameplay settings returned to Playing");
                Show("Paused"); Show("Controls"); Call("Back"); Require(Current() == "Paused", "Parent-routed Esc lost pause origin");
            });
            // Bug caught: SetSettings invokes persistence callbacks or controls emit wrong values.
            Test("SettingsHydrationIsSilentAndEditsEmitValues", () =>
            {
                int volumes = 0, shakes = 0; float volume = -1; bool shake = true;
                uiType.GetField("VolumeChanged").SetValue(ui, (Action<float>)(v => { volumes++; volume = v; }));
                uiType.GetField("ShakeChanged").SetValue(ui, (Action<bool>)(v => { shakes++; shake = v; }));
                Call("SetSettings", .7f, true); Require(volumes == 0 && shakes == 0, "Hydration emitted a user change");
                Node("VolumeSlider").GetComponent<UnityEngine.UI.Slider>().value = .25f;
                Node("ShakeToggle").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
                Require(volumes == 1 && Mathf.Approximately(volume, .25f) && shakes == 1 && !shake, "Wrong settings payload");
            });
            // Bug caught: stale objective/prompt or out-of-range health draws beyond bar.
            Test("HudDisplaysSnapshotAndClampsMeters", () =>
            {
                Show("Playing"); Call("UpdateHUD", Hud(160, 2));
                Require(Node("HealthFill").GetComponent<UnityEngine.UI.Image>().fillAmount == 1, "Health overflow");
                Require(Node("ObjectiveText").GetComponent<TMP_Text>().text.Contains("Activate the sigil"), "Objective not updated");
                Require(Node("PromptText").GetComponent<TMP_Text>().text.Contains("Inspect"), "Prompt not updated");
                Call("UpdateHUD", Hud(-10, 0)); Require(Node("HealthFill").GetComponent<UnityEngine.UI.Image>().fillAmount == 0, "Negative health");
            });
            // Bug caught: terminal buttons route to start instead of restart/menu or summary drops metrics.
            Test("TerminalScreensExposeSummaryAndCorrectRequests", () =>
            {
                int restart = 0, menu = 0;
                uiType.GetField("RestartRequested").SetValue(ui, (Action)(() => restart++));
                uiType.GetField("MenuRequested").SetValue(ui, (Action)(() => menu++));
                Call("SetSummary", 125f, true, 3); Show("Complete");
                var summary = Node("SummaryText").GetComponent<TMP_Text>().text;
                Require(summary.Contains("02:05") && summary.Contains("3") && summary.Contains("Defeated"), "Missing completion metrics");
                Click("CompleteRestartButton"); Click("CompleteMenuButton");
                Show("Dead"); Click("DeadRestartButton"); Click("DeadMenuButton");
                Require(restart == 2 && menu == 2, "Terminal request misrouted");
            });
            // Bug caught: toast panel never becomes visible when a paused modal is showing.
            Test("ToastVisibleWhilePausedAndEmptyToastClears", () =>
            {
                Show("Paused"); Call("ShowToast", "Sigil activated");
                Require(Node("Toast").gameObject.activeInHierarchy && Node("ToastText").GetComponent<TMP_Text>().text.Contains("Sigil"), "Toast absent");
                Call("ShowToast", ""); Require(!Node("Toast").gameObject.activeInHierarchy, "Empty toast remains visible");
            });
            // Bug caught: modal button anchors yield zero-size or clipped click targets.
            Test("ActiveButtonsStayInsideCanvasAndHaveKeyboardFocus", () =>
            {
                foreach (var view in new[] { "Menu", "Paused", "Inventory", "Settings", "Controls", "Dead", "Complete" })
                {
                    Show(view); Canvas.ForceUpdateCanvases();
                    var root = host.GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
                    foreach (var button in host.GetComponentsInChildren<UnityEngine.UI.Button>())
                    {
                        var rect = button.GetComponent<RectTransform>(); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                        Require(rect.rect.width > 0 && rect.rect.height > 0, "Zero-size " + button.name);
                        Require(corners.All(p => root.rect.Contains((Vector2)root.InverseTransformPoint(p))), "Clipped " + button.name);
                    }
                    var focus = EventSystem.current.currentSelectedGameObject;
                    Require(focus != null && focus.activeInHierarchy, "No active keyboard focus for " + view);
                }
                report.AppendLine("INFO Layout checked at current viewport " + Screen.width + "x" + Screen.height + "; parent must also run at 1280x720 and 1920x1080.");
            });
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            foreach (var system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                if (!oldSystems.Contains(system)) UnityEngine.Object.DestroyImmediate(system.gameObject);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected);
        }
        return "Slice UI: " + passed + " PASS, " + failed + " FAIL\n" + report;
    }

    static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
    static Action<T> ViewObserver<T>(Action<object> observe) => value => observe(value);
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
