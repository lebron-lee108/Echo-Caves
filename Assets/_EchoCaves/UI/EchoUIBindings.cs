using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EchoCaves.UI
{
    public enum EchoUIView { HUD, MainMenu, Pause, Settings, Dialogue, Map, SoundLibrary, Playback, Radio, Shop, Settlement, Fail, End }

    [Serializable]
    public sealed class EchoUIViewReference
    {
        public EchoUIView id;
        public GameObject panel;
        public UnityEngine.UI.Selectable firstSelected;
    }

    [Serializable] public sealed class EchoFloatEvent : UnityEvent<float> { }
    [Serializable] public sealed class EchoBoolEvent : UnityEvent<bool> { }
    [Serializable] public sealed class EchoStringEvent : UnityEvent<string> { }

    /// <summary>C-role presentation only. No gameplay, timers, audio, economy or persistence.</summary>
    [DisallowMultipleComponent]
    public sealed class EchoUIBindings : MonoBehaviour
    {
        [Header("Editable view references; authored hierarchy is the UI")]
        [SerializeField] private EchoUIViewReference[] views = Array.Empty<EchoUIViewReference>();
        [SerializeField] private EchoUIView currentView = EchoUIView.HUD;
        [SerializeField] private EchoUIView settingsReturnView = EchoUIView.Pause;
        public EchoUIViewReference[] Views => views;
        public EchoUIView CurrentView => currentView;

        [Header("STATIC EXAMPLE VALUES — not live game state")]
        public int radioDurationSeconds = 60;
        public int highlightStartSeconds = 15;
        public int highlightEndSeconds = 25;
        public int sampleCopies = 6;
        public int sampleUnitPrice = 4;
        public int sampleElectricity = 2;
        public int sampleNetIncome = 22;
        public int sampleUpgradePrice = 18;
        public int sampleAfterUpgrade = 4;
        public bool mastersRetained = true;

        [Header("Presentation labels for B to supply real text/state")]
        public TextMeshProUGUI objectiveLabel;
        public TextMeshProUGUI energyLabel;
        public TextMeshProUGUI dialogueSpeakerLabel;
        public TextMeshProUGUI dialogueBodyLabel;
        public TextMeshProUGUI feedbackLabel;
        public TextMeshProUGUI radioClockLabel;
        public TextMeshProUGUI balanceLabel;
        public RectTransform bossMarkers;
        public GameObject failPanel;

        [Header("B integration: intent only. Empty until programmer wires listeners")]
        public UnityEvent onNewGameRequested = new UnityEvent();
        public UnityEvent onContinueGameRequested = new UnityEvent();
        public UnityEvent onQuitRequested = new UnityEvent();
        public UnityEvent onResumeRequested = new UnityEvent();
        public UnityEvent onReturnToMenuRequested = new UnityEvent();
        public UnityEvent onDialogueContinueRequested = new UnityEvent();
        public UnityEvent onDialogueSkipRequested = new UnityEvent();
        public EchoStringEvent onAuditionRequested = new EchoStringEvent();
        public EchoStringEvent onPlaybackRequested = new EchoStringEvent();
        public UnityEvent onBroadcastRequested = new UnityEvent();
        public UnityEvent onHighlightRequested = new UnityEvent();
        public UnityEvent onPurchaseBatteryRequested = new UnityEvent();
        public UnityEvent onSettlementAcknowledgeRequested = new UnityEvent();
        public UnityEvent onRetryFromRestRequested = new UnityEvent();
        public UnityEvent onSaveAndReturnRequested = new UnityEvent();
        public EchoFloatEvent onMasterVolumeRequested = new EchoFloatEvent();
        public EchoFloatEvent onMusicVolumeRequested = new EchoFloatEvent();
        public EchoFloatEvent onEffectsVolumeRequested = new EchoFloatEvent();
        public EchoFloatEvent onAmbienceVolumeRequested = new EchoFloatEvent();
        public EchoBoolEvent onScreenShakeRequested = new EchoBoolEvent();
        public UnityEvent onTextSpeedRequested = new UnityEvent();

        public void ConfigureViews(EchoUIViewReference[] authoredViews) { views = authoredViews; }

        /// <summary>Only local preview behavior: activate one serialized view; never advances game state.</summary>
        public void ShowView(int id)
        {
            if (!Enum.IsDefined(typeof(EchoUIView), id)) throw new ArgumentOutOfRangeException(nameof(id));
            var target = Array.Find(views, v => v != null && (int)v.id == id);
            if (target == null || target.panel == null) throw new InvalidOperationException("Unbound Echo view: " + id);
            currentView = (EchoUIView)id;
            foreach (var view in views) if (view != null && view.panel != null) view.panel.SetActive(view == target);
            if (Application.isPlaying && EventSystem.current != null && EventSystem.current.gameObject.scene == gameObject.scene
                && target.firstSelected != null && target.firstSelected.IsInteractable())
                EventSystem.current.SetSelectedGameObject(target.firstSelected.gameObject);
        }

        public void OpenSettings()
        {
            if (currentView == EchoUIView.Settings) return;
            settingsReturnView = currentView;
            ShowView((int)EchoUIView.Settings);
        }
        public void BackFromSettings() { ShowView((int)settingsReturnView); }
        public void RequestNewGame() => onNewGameRequested.Invoke();
        public void RequestContinueGame() => onContinueGameRequested.Invoke();
        public void RequestQuit() => onQuitRequested.Invoke();
        public void RequestResume() => onResumeRequested.Invoke();
        public void RequestReturnToMenu() => onReturnToMenuRequested.Invoke();
        public void RequestDialogueContinue() => onDialogueContinueRequested.Invoke();
        public void RequestDialogueSkip() => onDialogueSkipRequested.Invoke();
        public void RequestAudition(string soundId) => onAuditionRequested.Invoke(soundId);
        public void RequestPlayback(string soundId) => onPlaybackRequested.Invoke(soundId);
        public void RequestBroadcast() => onBroadcastRequested.Invoke();
        public void RequestHighlight() => onHighlightRequested.Invoke();
        public void RequestPurchaseBattery() => onPurchaseBatteryRequested.Invoke();
        public void RequestSettlementAcknowledge() => onSettlementAcknowledgeRequested.Invoke();
        public void RequestRetryFromRest() => onRetryFromRestRequested.Invoke();
        public void RequestSaveAndReturn() => onSaveAndReturnRequested.Invoke();
        public void RequestMasterVolume(float value) => onMasterVolumeRequested.Invoke(value);
        public void RequestMusicVolume(float value) => onMusicVolumeRequested.Invoke(value);
        public void RequestEffectsVolume(float value) => onEffectsVolumeRequested.Invoke(value);
        public void RequestAmbienceVolume(float value) => onAmbienceVolumeRequested.Invoke(value);
        public void RequestScreenShake(bool value) => onScreenShakeRequested.Invoke(value);
        public void RequestTextSpeed() => onTextSpeedRequested.Invoke();
    }
}
