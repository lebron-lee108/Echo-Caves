using UnityEngine;

namespace VP515.Slice
{
    /// <summary>Presentation-only entrance. Unscaled timing never delays pause or button callbacks.</summary>
    [RequireComponent(typeof(RectTransform),typeof(CanvasGroup))]
    public sealed class SlicePanelMotion : MonoBehaviour
    {
        CanvasGroup group;
        RectTransform rect;
        Vector2 destination;
        float elapsed;
        bool initialized;
        public bool HasEntered { get; private set; }
        const float Duration=.20f;
        public void Enter()
        {
            if(!initialized){rect=GetComponent<RectTransform>();group=GetComponent<CanvasGroup>();destination=rect.anchoredPosition;initialized=true;}
            elapsed=0;HasEntered=true;group.alpha=0;
            rect.anchoredPosition=destination+Vector2.left*12;
        }
        void Update()
        {
            if(!HasEntered||elapsed>=Duration)return;
            elapsed=Mathf.Min(Duration,elapsed+Time.unscaledDeltaTime);
            float t=elapsed/Duration;float ease=1-Mathf.Pow(1-t,3);
            group.alpha=ease;rect.anchoredPosition=destination+Vector2.left*(12*(1-ease));
        }
    }
}
