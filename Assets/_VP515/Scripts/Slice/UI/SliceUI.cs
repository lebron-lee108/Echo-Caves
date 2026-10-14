using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace VP515.Slice
{
    /// <summary>Runtime-built uGUI presentation. Parent injects the TMP font, supplies snapshots,
    /// persists VolumeChanged/ShakeChanged, and controls time/input in ViewChanged.
    /// Requests do not change gameplay state. Parent must suppress gameplay input for the
    /// closing frame (especially Resume/Start/Dew clicks), then call Show(Playing).</summary>
    public sealed class SliceUI : MonoBehaviour
    {
        public Action StartRequested, ResumeRequested, RestartRequested, MenuRequested, QuitRequested, DewRequested;
        public Action<float> VolumeChanged;
        public Action<bool> ShakeChanged;
        public Action<SliceView> ViewChanged;
        public SliceView CurrentView { get; private set; } = SliceView.Menu;

        static readonly Color Parchment = new Color(.96f, .90f, .77f);
        static readonly Color Muted = new Color(.76f, .77f, .71f);
        static readonly Color Amber = new Color(1f, .72f, .34f);
        static readonly Color Cyan = new Color(.39f, .88f, .94f);
        static readonly Color Panel = new Color(.045f, .07f, .08f, .88f);
        static readonly Color ButtonIdle = new Color(.13f, .18f, .19f, .85f);
        readonly Dictionary<SliceView, RectTransform> panels = new Dictionary<SliceView, RectTransform>();
        readonly Dictionary<SliceView, List<UnityEngine.UI.Selectable>> navigation = new Dictionary<SliceView, List<UnityEngine.UI.Selectable>>();
        readonly Stack<SliceView> backStack = new Stack<SliceView>();
        TMP_FontAsset font;
        RectTransform canvasRoot, hud, scrim, tooltipRoot, toastRoot, promptRoot;
        TMP_Text healthText, energyText, objectiveText, promptText, dewText, dewStatus, formText, progressText;
        TMP_Text summaryText, tooltipText, toastText, volumeText;
        UnityEngine.UI.Image healthFill, energyFill;
        UnityEngine.UI.Button useDew;
        UnityEngine.UI.Slider volumeSlider;
        UnityEngine.UI.Toggle shakeToggle;
        CanvasGroup toastGroup;
        Sprite meterSprite;
        SliceHudData snapshot;
        bool built;
        float volume = .8f, toastUntil;
        bool shake = true;
        float summarySeconds;
        bool summaryDefeated;
        int summaryCollected;

        /// <summary>Idempotent; requires an imported/injected TMP font. Does not open import dialogs.</summary>
        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            if (font == null) throw new ArgumentNullException(nameof(font), "Inject an imported TMP_FontAsset.");
            this.font = font;
            var existing = EventSystem.current;
            if (existing == null) existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                var system = new GameObject("SliceEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            canvasRoot = Rect("SliceCanvas", transform);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasRoot.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            canvasRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            BuildHUD();

            scrim = Rect("ModalScrim", canvasRoot); Stretch(scrim);
            Image(scrim, new Color(.015f, .025f, .035f, .32f), true);
            foreach (SliceView view in Enum.GetValues(typeof(SliceView)))
            {
                if (view == SliceView.Playing) continue;
                var panel = Rect(view + "Panel", canvasRoot);
                panel.anchorMin = view == SliceView.Menu ? new Vector2(.055f, .10f) : new Vector2(.255f, .10f);
                panel.anchorMax = view == SliceView.Menu ? new Vector2(.43f, .93f) : new Vector2(.745f, .90f);
                panel.offsetMin = panel.offsetMax = Vector2.zero;
                Image(panel, Panel, true);
                panel.gameObject.AddComponent<SlicePanelMotion>();
                var line = Rect("AmberEdge", panel); line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(0, 1);
                line.pivot = new Vector2(0, .5f); line.sizeDelta = new Vector2(3, 0); Image(line, new Color(Amber.r, Amber.g, Amber.b, .7f));
                var layout = panel.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.padding = new RectOffset(38, 38, 32, 32); layout.spacing = 12;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.UpperLeft;
                // Decoration is not managed by the content layout.
                line.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                panels.Add(view, panel); navigation.Add(view, new List<UnityEngine.UI.Selectable>());
            }
            BuildMenu(); BuildPause(); BuildInventory(); BuildSettings(); BuildControls(); BuildTerminal();
            BuildContext();
            built = true;
            SetSettings(volume, shake);
            SetSummary(summarySeconds, summaryDefeated, summaryCollected);
            UpdateHUD(snapshot);
            Show(CurrentView);
        }

        public void Show(SliceView view)
        {
            if (!Enum.IsDefined(typeof(SliceView), view)) throw new ArgumentOutOfRangeException(nameof(view));
            if (view != CurrentView)
            {
                if (view == SliceView.Settings || view == SliceView.Controls)
                    backStack.Push(CurrentView == SliceView.Playing ? SliceView.Paused : CurrentView);
                else backStack.Clear();
            }
            CurrentView = view;
            if (built)
            {
                foreach (var entry in panels)
                {
                    bool wasVisible=entry.Value.gameObject.activeSelf;
                    bool visible=entry.Key==view;
                    entry.Value.gameObject.SetActive(visible);
                    if(visible){var motion=entry.Value.GetComponent<SlicePanelMotion>();if(!wasVisible||!motion.HasEntered)motion.Enter();}
                }
                bool inGame = view == SliceView.Playing || view == SliceView.Paused || view == SliceView.Inventory ||
                    ((view == SliceView.Settings || view == SliceView.Controls) && backStack.Count > 0 && IsGameplayOrigin());
                hud.gameObject.SetActive(inGame);
                scrim.gameObject.SetActive(view != SliceView.Playing);
                SetTooltip("");
                if (view == SliceView.Playing)
                {
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                }
                else ConfigureNavigation(view);
            }
            // Emit even for the same view: parent can synchronize its authoritative session state.
            ViewChanged?.Invoke(view);
        }

        bool IsGameplayOrigin()
        {
            foreach (var view in backStack)
                if (view == SliceView.Paused || view == SliceView.Inventory || view == SliceView.Playing) return true;
            return false;
        }

        /// <summary>Parent may route Esc here in Settings/Controls. Other views are unaffected.</summary>
        public void Back()
        {
            if (CurrentView != SliceView.Settings && CurrentView != SliceView.Controls) return;
            var view = backStack.Count > 0 ? backStack.Pop() : SliceView.Menu;
            // Do not push the page we are leaving when returning to another subpanel.
            CurrentView = view;
            Show(view);
        }

        public void UpdateHUD(SliceHudData data)
        {
            snapshot = data;
            if (!built) return;
            healthFill.fillAmount = Mathf.Clamp01(Finite(data.Health) / 100f);
            energyFill.fillAmount = Mathf.Clamp01(Finite(data.Energy) / 100f);
            healthText.text = "HEALTH  " + Mathf.CeilToInt(Mathf.Clamp(Finite(data.Health), 0, 100));
            energyText.text = "ENERGY  " + Mathf.CeilToInt(Mathf.Clamp(Finite(data.Energy), 0, 100));
            formText.text = data.Butterfly ? "BUTTERFLY  /  Q Return" : "BODY  /  Q Transform";
            formText.color = data.Butterfly ? Cyan : Parchment;
            objectiveText.text = string.IsNullOrWhiteSpace(data.Objective) ? "Explore the path ahead" : data.Objective;
            promptText.text = data.Prompt ?? "";
            promptRoot.gameObject.SetActive(!string.IsNullOrWhiteSpace(data.Prompt));
            dewText.text = "DEW  " + Mathf.Max(0, data.DewCount) + "    [Tab]";
            progressText.text = (data.GateUnlocked ? "SIGIL  Activated" : "SIGIL  Unlit") + "     /     " +
                (data.EnemyDefeated ? "GUARDIAN  Defeated" : "GUARDIAN  Ahead") + "     /     " + FormatTime(data.Elapsed);
            useDew.interactable = CanUseDew();
            dewStatus.text = DewContext();
            useDew.GetComponentInChildren<TMP_Text>().text = CanUseDew() ? "Use Dew   /   " + data.DewCount + " available" : "Dew Unavailable";
            if (CurrentView == SliceView.Inventory) ConfigureNavigation(CurrentView);
        }

        bool CanUseDew() => snapshot.DewCount > 0 && snapshot.Health > 0 && snapshot.Health < 100 && !snapshot.Butterfly;
        string DewContext()
        {
            if (snapshot.DewCount <= 0) return "No dew collected. Look for a glowing droplet along the path.";
            if (snapshot.Health <= 0) return "You have fallen. Restart to continue your journey.";
            if (snapshot.Butterfly) return "Return to your body with Q before using dew.";
            if (snapshot.Health >= 100) return "Health is full. Keep this dew for when you need it.";
            return "Restore health by consuming one dew. The world stays paused while your bag is open.";
        }

        public void ShowToast(string message)
        {
            if (!built) return;
            bool visible = !string.IsNullOrWhiteSpace(message);
            toastText.text = message ?? ""; toastRoot.gameObject.SetActive(visible);
            toastGroup.alpha = visible ? 1 : 0;
            toastUntil = Time.unscaledTime + 3.4f;
        }

        void Update()
        {
            if (!built || !toastRoot.gameObject.activeSelf) return;
            float remaining = toastUntil - Time.unscaledTime;
            toastGroup.alpha = Mathf.Clamp01(remaining / .45f);
            if (remaining <= 0) toastRoot.gameObject.SetActive(false);
        }

        public void SetSummary(float seconds, bool enemyDefeated, int collected)
        {
            summarySeconds = seconds; summaryDefeated = enemyDefeated; summaryCollected = collected;
            if (summaryText == null) return;
            summaryText.text = "JOURNEY TIME     " + FormatTime(seconds) + "\n\nGUARDIAN     " +
                (enemyDefeated ? "Defeated" : "Not defeated") + "\n\nDEW COLLECTED     " + Mathf.Max(0, collected);
        }

        /// <summary>Silent hydration; parent owns PlayerPrefs/audio and persistence.</summary>
        public void SetSettings(float volume, bool shake)
        {
            this.volume = Mathf.Clamp01(Finite(volume)); this.shake = shake;
            if (volumeSlider == null) return;
            volumeSlider.SetValueWithoutNotify(this.volume);
            shakeToggle.SetIsOnWithoutNotify(shake);
            UpdateVolumeLabel(this.volume);
        }

        void BuildMenu()
        {
            var panel = panels[SliceView.Menu];
            Label(panel, "MenuEyebrow", "CHAPTER ONE  /  PLAYABLE SLICE", 22, Cyan, 36);
            var title = Label(panel, "Title", "VP515", 132, Parchment, 158);
            title.characterSpacing = 9; title.fontStyle = FontStyles.Bold;
            Label(panel, "MenuDescription", "One body. A butterfly. A path through the forest.", 28, Muted, 80);
            Rule(panel);
            Button(panel, SliceView.Menu, "StartButton", "Start Journey", () => StartRequested?.Invoke(), "Begin the first section from the forest entrance.", true);
            Button(panel, SliceView.Menu, "MenuControlsButton", "Controls", () => Show(SliceView.Controls), "Movement, transformation, combat and interaction.");
            Button(panel, SliceView.Menu, "MenuSettingsButton", "Settings", () => Show(SliceView.Settings), "Adjust master volume and camera shake.");
            Button(panel, SliceView.Menu, "QuitButton", "Quit", () => QuitRequested?.Invoke(), "Close the game.");
            Label(panel, "MenuNavigationHint", "Arrow keys to select  /  Enter to confirm", 21, Muted, 38);
        }

        void BuildPause()
        {
            var panel = panels[SliceView.Paused]; Header(panel, "PAUSED", "Take a breath. Your journey is waiting.");
            Button(panel, SliceView.Paused, "ResumeButton", "Resume Journey", () => ResumeRequested?.Invoke(), "Return to the world.", true);
            Button(panel, SliceView.Paused, "PauseInventoryButton", "Inventory", () => Show(SliceView.Inventory), "Inspect your dew without advancing the world.");
            Button(panel, SliceView.Paused, "PauseControlsButton", "Controls", () => Show(SliceView.Controls), "Review controls while remaining paused.");
            Button(panel, SliceView.Paused, "PauseSettingsButton", "Settings", () => Show(SliceView.Settings), "Volume and camera shake. Returning keeps the world paused.");
            Button(panel, SliceView.Paused, "PauseRestartButton", "Restart Section", () => RestartRequested?.Invoke(), "Start this section again; section progress will reset.");
            Button(panel, SliceView.Paused, "PauseMenuButton", "Main Menu", () => MenuRequested?.Invoke(), "Leave this run and return to the menu.");
        }

        void BuildInventory()
        {
            var panel = panels[SliceView.Inventory]; Header(panel, "INVENTORY", "Collected along the path. Use with care.");
            Label(panel, "DewItemTitle", "FOREST DEW", 42, Cyan, 72);
            Label(panel, "DewDescription", "A small drop of warmth.\nUse one to restore health in your body form.", 30, Parchment, 112);
            Rule(panel);
            dewStatus = Label(panel, "DewStatus", "No dew collected.", 27, Muted, 122);
            useDew = Button(panel, SliceView.Inventory, "UseDewButton", "Use Dew", () =>
            {
                if (CurrentView == SliceView.Inventory && CanUseDew()) DewRequested?.Invoke();
            }, "", true);
            useDew.GetComponent<SliceUIState>().Bind(useDew, useDew.GetComponentInChildren<TMP_Text>(),
                useDew.transform.Find("SelectionAccent").GetComponent<RectTransform>(), Parchment, Amber, SetTooltip, DewContext);
            Button(panel, SliceView.Inventory, "InventoryCloseButton", "Return to Journey", () => ResumeRequested?.Invoke(), "Close your bag and continue. Tab also closes the bag.");
        }

        void BuildSettings()
        {
            var panel = panels[SliceView.Settings]; Header(panel, "SETTINGS", "Make the journey comfortable.");
            volumeText = Label(panel, "VolumeLabel", "MASTER VOLUME", 30, Parchment, 54);
            var sliderRect = Rect("VolumeSlider", panel); Height(sliderRect, 64);
            Image(sliderRect, new Color(.10f, .16f, .18f, .9f), true);
            volumeSlider = sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            volumeSlider.minValue = 0; volumeSlider.maxValue = 1; volumeSlider.wholeNumbers = false;
            var track = Rect("Track", sliderRect); Stretch(track, 24, 24, 23, 23); Image(track, new Color(.29f, .35f, .35f));
            var fillArea = Rect("FillArea", sliderRect); Stretch(fillArea, 24, 24, 23, 23);
            var fill = Rect("Fill", fillArea); Stretch(fill); Image(fill, Amber);
            var handleArea = Rect("HandleArea", sliderRect); Stretch(handleArea, 24, 24, 0, 0);
            var handle = Rect("Handle", handleArea); handle.sizeDelta = new Vector2(22, 44); Image(handle, Parchment);
            volumeSlider.fillRect = fill; volumeSlider.handleRect = handle;
            volumeSlider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            volumeSlider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            volumeSlider.onValueChanged.AddListener(value => { volume = value; UpdateVolumeLabel(value); VolumeChanged?.Invoke(value); });
            Feedback(volumeSlider, volumeText, null, "Left / Right to adjust master volume.");
            navigation[SliceView.Settings].Add(volumeSlider);
            Label(panel, "VolumeHelp", "Affects music, ambience and sound effects.", 25, Muted, 56);
            Rule(panel);
            var toggleRect = Rect("ShakeToggle", panel); Height(toggleRect, 80); Image(toggleRect, ButtonIdle, true);
            shakeToggle = toggleRect.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            var box = Rect("Checkbox", toggleRect); box.anchorMin = box.anchorMax = new Vector2(0, .5f);
            box.pivot = new Vector2(0, .5f); box.anchoredPosition = new Vector2(22, 0); box.sizeDelta = new Vector2(36, 36);
            Image(box, new Color(.25f, .33f, .34f));
            var mark = Rect("Checkmark", box); Stretch(mark, 8, 8, 8, 8); var markImage = Image(mark, Amber);
            var label = Text("ShakeLabel", toggleRect, "Camera Shake", 30, Parchment);
            Stretch(label.rectTransform, 80, 18, 0, 0);
            shakeToggle.targetGraphic = box.GetComponent<UnityEngine.UI.Image>(); shakeToggle.graphic = markImage;
            shakeToggle.onValueChanged.AddListener(value => { shake = value; ShakeChanged?.Invoke(value); });
            Feedback(shakeToggle, label, null, "Toggle impact shake. Disable for a steadier camera.");
            navigation[SliceView.Settings].Add(shakeToggle);
            Label(panel, "ShakeHelp", "Optional camera feedback on combat impacts.\nYour preferences are saved automatically.", 25, Muted, 96);
            Button(panel, SliceView.Settings, "SettingsBackButton", "Back", Back, "Return to the previous page. Gameplay stays paused.", true);
        }

        void BuildControls()
        {
            var panel = panels[SliceView.Controls]; Header(panel, "CONTROLS", "Keyboard & mouse  /  Windows PC");
            panel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 8;
            ControlRow(panel, "Move", "A / D  or  Arrow keys");
            ControlRow(panel, "Jump", "Space");
            ControlRow(panel, "Attack", "J  or  Left mouse button");
            ControlRow(panel, "Transform / Return", "Q");
            ControlRow(panel, "Butterfly flight", "W A S D  or  Arrow keys");
            ControlRow(panel, "Inspect / Activate", "E");
            ControlRow(panel, "Inventory", "Tab");
            ControlRow(panel, "Pause / Back", "Esc");
            Label(panel, "ControlsTip", "Butterfly flight consumes energy. Return to your body to fight.\nIn menus: Arrow keys to select, Enter to confirm.", 25, Cyan, 80);
            Button(panel, SliceView.Controls, "ControlsBackButton", "Back", Back, "Return to the previous page. Gameplay stays paused.", true);
        }

        void BuildTerminal()
        {
            var dead = panels[SliceView.Dead]; Header(dead, "YOU HAVE FALLEN", "A quiet pause. Begin again when you are ready.");
            Label(dead, "DeathDescription", "Return to your checkpoint to try again.\nThe awakened gate stays open.", 32, Muted, 156);
            Button(dead, SliceView.Dead, "DeadRestartButton", "Return to Checkpoint", () => RestartRequested?.Invoke(), "Recover at the lantern and keep your progress.", true);
            Button(dead, SliceView.Dead, "DeadMenuButton", "Main Menu", () => MenuRequested?.Invoke(), "Return to the main menu.");
            var complete = panels[SliceView.Complete]; Header(complete, "SECTION COMPLETE", "The path opens. A small journey, carried through.");
            summaryText = Label(complete, "SummaryText", "", 32, Parchment, 282);
            Rule(complete);
            Button(complete, SliceView.Complete, "CompleteRestartButton", "Play Again", () => RestartRequested?.Invoke(), "Replay the section with fresh progress.", true);
            Button(complete, SliceView.Complete, "CompleteMenuButton", "Main Menu", () => MenuRequested?.Invoke(), "Return to the main menu.");
        }

        void BuildHUD()
        {
            hud = Rect("HUD", canvasRoot); Stretch(hud);
            var vitals = Rect("Vitals", hud); TopPanel(vitals, .025f, .225f, 112); Image(vitals, new Color(Panel.r, Panel.g, Panel.b, .30f));
            healthText = Text("HealthText", vitals, "HEALTH", 23, Parchment); TopText(healthText.rectTransform, 16, 6, 26);
            healthFill = Meter(vitals, "HealthFill", Amber, 35);
            energyText = Text("EnergyText", vitals, "ENERGY", 21, Parchment); TopText(energyText.rectTransform, 16, 60, 24);
            energyFill = Meter(vitals, "EnergyFill", Cyan, 88);
            var objective = Rect("Objective", hud); TopPanel(objective, .355f, .705f, 82); Image(objective, new Color(Panel.r, Panel.g, Panel.b, .20f));
            var caption = Text("ObjectiveCaption", objective, "THE WITHERED CROSSING", 17, Amber); TopText(caption.rectTransform, 18, 5, 24);
            objectiveText = Text("ObjectiveText", objective, "Explore the path ahead", 24, Parchment); TopText(objectiveText.rectTransform, 18, 33, 36);
            progressText = Text("ProgressText", objective, "", 19, Muted); TopText(progressText.rectTransform, 20, 112, 28);
            progressText.gameObject.SetActive(false);
            var bag = Rect("Bag", hud); TopPanel(bag, .82f, .975f, 100); Image(bag, new Color(Panel.r, Panel.g, Panel.b, .30f));
            formText = Text("FormText", bag, "BODY", 21, Cyan); TopText(formText.rectTransform, 16, 10, 34);
            var bagButton = Rect("HudInventoryButton", bag); bagButton.anchorMin = new Vector2(0, 0); bagButton.anchorMax = new Vector2(1, 0);
            bagButton.pivot = new Vector2(.5f, 0); bagButton.offsetMin = new Vector2(12, 8); bagButton.offsetMax = new Vector2(-12, 54);
            var target = Image(bagButton, new Color(.10f,.16f,.18f,.38f), true); var button = bagButton.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = target; ApplyColors(button);
            button.onClick.AddListener(() => { if (CurrentView == SliceView.Playing) Show(SliceView.Inventory); });
            // HUD must not steal keyboard navigation from a modal.
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            dewText = Text("DewText", bagButton, "DEW  0    [Tab]", 22, Parchment); Stretch(dewText.rectTransform, 12, 12, 0, 0);
            dewText.alignment = TextAlignmentOptions.Center;
            var hints = Text("HudHints", hud, "A / D  Move     Space  Jump     J  Strike     Q  Butterfly     E  Interact     Tab  Satchel     Esc  Pause", 19, Muted);
            hints.alignment = TextAlignmentOptions.Center; Bottom(hints.rectTransform, .03f, .97f, 18, 34);
            promptRoot = Rect("Prompt", hud); Bottom(promptRoot, .24f, .76f, 57, 60); Image(promptRoot, new Color(Panel.r, Panel.g, Panel.b, .52f));
            promptText = Text("PromptText", promptRoot, "", 24, Parchment); Stretch(promptText.rectTransform, 20, 20, 8, 8); promptText.alignment = TextAlignmentOptions.Center;
        }

        void BuildContext()
        {
            tooltipRoot = Rect("Tooltip", canvasRoot); Bottom(tooltipRoot, .10f, .90f, 20, 64);
            Image(tooltipRoot, new Color(.035f, .06f, .07f, .91f));
            tooltipText = Text("TooltipText", tooltipRoot, "", 24, Parchment); Stretch(tooltipText.rectTransform, 20, 20, 6, 6); tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipRoot.gameObject.SetActive(false);
            toastRoot = Rect("Toast", canvasRoot); toastRoot.anchorMin = new Vector2(.26f, 1); toastRoot.anchorMax = new Vector2(.74f, 1);
            toastRoot.pivot = new Vector2(.5f, 1); toastRoot.offsetMin = new Vector2(0, -206); toastRoot.offsetMax = new Vector2(0, -138);
            Image(toastRoot, new Color(.04f, .09f, .10f, .72f));
            toastText = Text("ToastText", toastRoot, "", 23, Cyan); Stretch(toastText.rectTransform, 22, 22, 8, 8); toastText.alignment = TextAlignmentOptions.Center;
            toastGroup = toastRoot.gameObject.AddComponent<CanvasGroup>(); toastGroup.blocksRaycasts = false; toastGroup.interactable = false;
            toastRoot.gameObject.SetActive(false);
        }

        void SetTooltip(string context)
        {
            if (tooltipRoot == null) return;
            tooltipText.text = context ?? "";
            tooltipRoot.gameObject.SetActive(CurrentView != SliceView.Playing && !string.IsNullOrWhiteSpace(context));
        }

        void ConfigureNavigation(SliceView view)
        {
            if (!navigation.TryGetValue(view, out var controls)) return;
            var available = controls.FindAll(s => s.gameObject.activeInHierarchy && s.IsInteractable());
            for (int i = 0; i < available.Count; i++)
            {
                var nav = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit };
                nav.selectOnUp = available[(i + available.Count - 1) % available.Count];
                nav.selectOnDown = available[(i + 1) % available.Count];
                // Slider left/right remains its value adjustment, not a focus change.
                available[i].navigation = nav;
            }
            var system = EventSystem.current;
            if (system == null || available.Count == 0) return;
            var selected = system.currentSelectedGameObject;
            if (selected != null && available.Exists(s => s.gameObject == selected)) return;
            system.SetSelectedGameObject(null);
            system.SetSelectedGameObject(available[0].gameObject);
        }

        void Header(RectTransform panel, string title, string subtitle)
        {
            Label(panel, panel.name + "Eyebrow", "VP515  /  CHAPTER ONE", 21, Cyan, 32);
            Label(panel, panel.name + "Title", title, 52, Parchment, 90).fontStyle = FontStyles.Bold;
            Label(panel, panel.name + "Subtitle", subtitle, 27, Muted, 72); Rule(panel);
        }

        void ControlRow(RectTransform parent, string action, string keys)
        {
            var row = Rect(action.Replace(" ", "") + "Row", parent); Height(row, 40);
            var left = Text("Action", row, action, 26, Parchment);
            left.rectTransform.anchorMin = new Vector2(0, 0); left.rectTransform.anchorMax = new Vector2(.43f, 1);
            left.rectTransform.offsetMin = left.rectTransform.offsetMax = Vector2.zero;
            var right = Text("Keys", row, keys, 26, Cyan);
            right.rectTransform.anchorMin = new Vector2(.45f, 0); right.rectTransform.anchorMax = new Vector2(1, 1);
            right.rectTransform.offsetMin = right.rectTransform.offsetMax = Vector2.zero;
        }

        UnityEngine.UI.Button Button(RectTransform parent, SliceView view, string name, string title, Action action, string context, bool primary = false)
        {
            var rect = Rect(name, parent); Height(rect, 66);
            var image = Image(rect, primary ? new Color(.29f, .23f, .14f, .90f) : ButtonIdle, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; ApplyColors(button);
            button.onClick.AddListener(() => { if (button.IsInteractable()) action?.Invoke(); });
            var label = Text("Label", rect, title, 30, Parchment); Stretch(label.rectTransform, 28, 18, 4, 4);
            var marker = Rect("SelectionAccent", rect); marker.anchorMin = new Vector2(0, 0); marker.anchorMax = new Vector2(0, 1);
            marker.pivot = new Vector2(0, .5f); marker.sizeDelta = new Vector2(4, -16); Image(marker, Amber);
            Feedback(button, label, marker, context);
            navigation[view].Add(button);
            return button;
        }

        void Feedback(UnityEngine.UI.Selectable control, TMP_Text label, RectTransform marker, string context)
        {
            control.gameObject.AddComponent<SliceUIState>().Bind(control, label, marker, Parchment, Amber, SetTooltip, () => context);
        }

        static void ApplyColors(UnityEngine.UI.Selectable control)
        {
            var colors = control.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.13f, .92f);
            colors.selectedColor = new Color(1.25f, 1.16f, .94f); colors.pressedColor = new Color(.64f, .78f, .80f);
            colors.disabledColor = new Color(.42f, .47f, .48f, .65f); colors.fadeDuration = .10f;
            control.colors = colors;
        }

        TMP_Text Label(RectTransform parent, string name, string text, float size, Color color, float height)
        {
            var label = Text(name, parent, text, size, color); Height(label.rectTransform, height); return label;
        }

        TMP_Text Text(string name, Transform parent, string text, float size, Color color)
        {
            var rect = Rect(name, parent); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.enableAutoSizing = true; label.fontSizeMax = size; label.fontSizeMin = size * .78f;
            label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
            label.richText = false; label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.localScale = Vector3.one;
            return rect;
        }

        static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }

        static void Stretch(RectTransform rect, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        static void Height(RectTransform rect, float height)
        {
            var element = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = height; element.flexibleHeight = 0;
        }

        static void Rule(RectTransform parent)
        {
            var rect = Rect("Divider", parent); Height(rect, 2); Image(rect, new Color(Amber.r, Amber.g, Amber.b, .30f));
        }

        static void TopPanel(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(left, 1); rect.anchorMax = new Vector2(right, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(0, -height - 28); rect.offsetMax = new Vector2(0, -28);
        }

        static void TopText(RectTransform rect, float inset, float top, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(inset, -top - height); rect.offsetMax = new Vector2(-inset, -top);
        }

        static void Bottom(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = new Vector2(left, 0); rect.anchorMax = new Vector2(right, 0); rect.pivot = new Vector2(.5f, 0);
            rect.offsetMin = new Vector2(0, bottom); rect.offsetMax = new Vector2(0, bottom + height);
        }

        UnityEngine.UI.Image Meter(RectTransform parent, string name, Color color, float top)
        {
            var background = Rect(name + "Track", parent); TopText(background, 16, top, 12); Image(background, new Color(.20f, .26f, .27f));
            var fill = Rect(name, background); Stretch(fill); var image = Image(fill, color);
            // Filled images need a sprite; the built-in white texture needs no asset import.
            if (meterSprite == null)
            {
                meterSprite = Sprite.Create(Texture2D.whiteTexture, new UnityEngine.Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
                meterSprite.name = "SliceMeterSprite"; meterSprite.hideFlags = HideFlags.HideAndDontSave;
            }
            image.sprite = meterSprite;
            image.type = UnityEngine.UI.Image.Type.Filled; image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; image.fillOrigin = 0;
            return image;
        }

        void UpdateVolumeLabel(float value) { if (volumeText != null) volumeText.text = "MASTER VOLUME     " + Mathf.RoundToInt(value * 100) + "%"; }
        void OnDestroy()
        {
            if (meterSprite == null) return;
            if (Application.isPlaying) Destroy(meterSprite); else DestroyImmediate(meterSprite);
        }
        static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : value;
        static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(Mathf.Clamp(Finite(seconds), 0, 359999));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
