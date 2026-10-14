using UnityEngine;
namespace VP515.Slice
{
    public sealed class SliceDrift:MonoBehaviour
    {
        public Vector2 Range=new Vector2(2,.3f);
        public float Speed=.2f,Phase;
        Vector3 origin;
        void Start(){origin=transform.localPosition;}
        void Update(){float t=Time.unscaledTime*Speed+Phase;transform.localPosition=origin+new Vector3(Mathf.Sin(t)*Range.x,Mathf.Sin(t*.7f)*Range.y,0);}
    }
}
