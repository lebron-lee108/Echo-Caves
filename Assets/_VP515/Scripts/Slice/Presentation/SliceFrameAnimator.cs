using UnityEngine;
namespace VP515.Slice
{
    public sealed class SliceFrameAnimator:MonoBehaviour
    {
        public SlicePlayer Player;public Sprite[] Frames,WalkFrames;public SliceAudio Sound;
        SpriteRenderer body;float steps,attack,damage,stride;Vector2 previous;bool wasGrounded;
        void Start(){Player.AnimatePose=false;body=Player.BodyRenderer;previous=Player.BodyPosition;Player.AttackPerformed+=OnAttack;Player.Damaged+=OnDamage;}
        void OnAttack(Vector2 p){attack=.25f;}
        void OnDamage(float amount){damage=.2f;}
        void LateUpdate()
        {
            if(!body||Frames==null||Frames.Length<8||!Player.CanAct)return;
            attack=Mathf.Max(0,attack-Time.deltaTime);damage=Mathf.Max(0,damage-Time.deltaTime);
            float movement=Mathf.Abs(Player.BodyPosition.x-previous.x);previous=Player.BodyPosition;int frame=0;
            if(!Player.Rules.Butterfly){if(damage>0)frame=7;else if(attack>0)frame=attack>.16f?4:5;else if(!Player.IsGrounded)frame=3;
                else if(movement>.001f){frame=(int)(Time.time*8)%2+1;steps+=movement;if(steps>.55f){steps=0;Sound.Step();}}else if(!wasGrounded)frame=6;}
            body.sprite=Frames[frame];
            if(frame==1||frame==2){stride+=movement;if(WalkFrames!=null&&WalkFrames.Length==8)body.sprite=WalkFrames[(int)(stride/.14f)%8];}
            body.flipX=Player.Facing<0;
            body.color=damage>0?new Color(1,.5f,.4f):Player.Rules.Butterfly?new Color(.55f,.65f,.7f,.75f):Color.white;
            wasGrounded=Player.IsGrounded;
        }
        void OnDestroy(){if(Player){Player.AttackPerformed-=OnAttack;Player.Damaged-=OnDamage;}}
    }
}
