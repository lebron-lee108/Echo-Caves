using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VP515.Slice
{
    public enum SliceView { Menu, Playing, Paused, Inventory, Settings, Controls, Dead, Complete }

    /// <summary>Health and Energy are 0..100. Elapsed is seconds. Snapshot is presentation-only.</summary>
    public struct SliceHudData
    {
        public float Health, Energy, Elapsed;
        public int DewCount;
        public string Objective, Prompt;
        public bool Butterfly, GateUnlocked, EnemyDefeated;
    }

    /// <summary>Pointer and EventSystem navigation share the same contextual feedback.
    /// No gameplay input polling; the parent's controller owns Esc/Tab and pause.</summary>
    public sealed class SliceUIState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        UnityEngine.UI.Selectable control;
        TMP_Text label;
        RectTransform marker;
        Action<string> tooltip;
        Func<string> context;
        Color idle, focus;
        bool hovered, selected;

        public void Bind(UnityEngine.UI.Selectable selectable, TMP_Text text, RectTransform accent,
            Color normal, Color highlighted, Action<string> showContext, Func<string> description)
        {
            control = selectable; label = text; marker = accent;
            idle = normal; focus = highlighted; tooltip = showContext; context = description;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
        public void OnSelect(BaseEventData data) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData data) { selected = false; Refresh(); }
        void OnDisable() { hovered = selected = false; if (marker != null) marker.gameObject.SetActive(false); }

        void Refresh()
        {
            bool active = hovered || selected;
            bool available = control == null || control.IsInteractable();
            if (label != null) label.color = available ? (active ? focus : idle) : new Color(idle.r, idle.g, idle.b, .48f);
            if (marker != null) marker.gameObject.SetActive(active && available);
            if (active) tooltip?.Invoke(context?.Invoke() ?? "");
            else tooltip?.Invoke("");
        }
    }
}
