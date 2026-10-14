using UnityEngine;
namespace VP515.Slice
{
    public sealed class SliceFade:MonoBehaviour
    {
        public SpriteRenderer Visual;public float Duration=.3f;
        Color color;float elapsed;
        void Start(){if(Visual)color=Visual.color;}
        void Update(){elapsed+=Time.deltaTime;if(Visual)Visual.color=new Color(color.r,color.g,color.b,color.a*(1-Mathf.Clamp01(elapsed/Duration)));if(elapsed>=Duration)Destroy(gameObject);}
    }
}
