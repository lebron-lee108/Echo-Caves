using UnityEngine;

namespace VP515.Slice
{
    public sealed class SliceCamera : MonoBehaviour
    {
        public SlicePlayer Player;
        public bool Following, ShakeEnabled=true;
        public float Left=9.6f,Right=50.4f;
        public float Height=1.3f;
        float velocity, shake;
        public void Impact(float amount) { if(ShakeEnabled) shake=Mathf.Max(shake,amount); }
        public void ResetView() { transform.position=new Vector3(Left,Height,-10); velocity=shake=0; }
        void LateUpdate()
        {
            float target=Following&&Player?Mathf.Clamp(Player.ActivePosition.x+Player.Facing*1.2f,Left,Right):Left;
            float x=Mathf.SmoothDamp(transform.position.x,target,ref velocity,.2f,100,Time.unscaledDeltaTime);
            shake=Mathf.MoveTowards(shake,0,Time.unscaledDeltaTime*.4f);
            float offset=ShakeEnabled&&Following?Mathf.Sin(Time.unscaledTime*70)*shake:0;
            transform.position=new Vector3(x,Height+offset,-10);
        }
    }

}
