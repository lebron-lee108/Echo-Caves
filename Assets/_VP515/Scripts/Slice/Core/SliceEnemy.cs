using System;
using UnityEngine;

namespace VP515.Slice
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
    public sealed class SliceEnemy : MonoBehaviour
    {
        public bool CanAct { get { return canAct; } set { canAct=value; if(body) body.simulated=value && (!player || !player.Rules.EnemyDefeated); } }
        public event Action HitConfirmed;
        public event Action Defeated;
        enum Phase { Patrol, Windup, Lunge, Recovery }
        SlicePlayer player; Rigidbody2D body; CapsuleCollider2D capsule; SpriteRenderer visual;
        float left,right,direction=1,timer,flash,pose; Vector2 start; Phase phase; bool canAct=true,lungeHit;
        Sprite[] frames;float deathFade;
        public void SetFrames(Sprite[] value){frames=value;if(frames!=null&&frames.Length==8)visual.sprite=frames[0];}
        void Awake()
        {
            body=GetComponent<Rigidbody2D>(); capsule=GetComponent<CapsuleCollider2D>(); body.freezeRotation=true;
            body.gravityScale=3; body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            body.interpolation=RigidbodyInterpolation2D.Interpolate; capsule.size=new Vector2(0.9f,1.3f); capsule.offset=new Vector2(0,0.65f);
            visual=new GameObject("EnemySprite").AddComponent<SpriteRenderer>(); visual.transform.SetParent(transform,false);
        }
        public void Initialize(SlicePlayer target,Sprite enemy,float patrolLeft,float patrolRight)
        {
            player=target; left=Mathf.Min(patrolLeft,patrolRight); right=Mathf.Max(patrolLeft,patrolRight); start=body.position;
            visual.sprite=enemy; visual.sortingOrder=10;
            if(enemy) { float s=1.45f/Mathf.Max(0.01f,enemy.bounds.size.y); visual.transform.localScale=Vector3.one*s; visual.transform.localPosition=Vector3.up*(-enemy.bounds.min.y*s); }
            ResetEnemy();
        }
        // Rules.Reset() owns restoring the enemy's 3 HP; this resets its physical state only.
        public void ResetEnemy()
        {
            phase=Phase.Patrol; timer=flash=pose=deathFade=0; direction=1; lungeHit=false;transform.position=new Vector3(start.x,start.y,transform.position.z);body.position=start;
            body.linearVelocity=Vector2.zero; bool alive=!player || !player.Rules.EnemyDefeated;
            visual.enabled=capsule.enabled=alive; visual.color=Color.white; body.simulated=CanAct && alive;
            if(frames!=null&&frames.Length==8)visual.sprite=frames[0];
            Physics2D.SyncTransforms();
        }
        void FixedUpdate()
        {
            bool advancing=CanAct && player && player.CanAct && player.Rules.Alive && !player.Rules.EnemyDefeated;
            if(player&&player.Rules.EnemyDefeated&&CanAct){deathFade+=Time.fixedDeltaTime;visual.color=new Color(.6f,.65f,.65f,Mathf.Clamp01(1-deathFade/1.1f));if(deathFade>1.1f)visual.enabled=false;}
            if(body.simulated!=advancing)body.simulated=advancing; if(!advancing) return;
            float dt=Time.fixedDeltaTime; timer-=dt; flash=Mathf.Max(0,flash-dt); pose+=dt;
            Vector2 delta=player.BodyPosition-body.position;
            if(phase==Phase.Patrol) {
                if(body.position.x<=left) direction=1; else if(body.position.x>=right) direction=-1;
                if(Mathf.Abs(delta.x)<3.5f && Mathf.Abs(delta.y)<1.2f) { direction=delta.x>=0?1:-1; phase=Phase.Windup; timer=0.5f; }
            } else if(timer<=0) {
                if(phase==Phase.Windup) { phase=Phase.Lunge; timer=0.32f; lungeHit=false; }
                else if(phase==Phase.Lunge) { phase=Phase.Recovery; timer=0.8f; }
                else phase=Phase.Patrol;
            }
            float speed=phase==Phase.Patrol?1.2f:phase==Phase.Lunge?7:0;
            if(flash<=0) body.linearVelocity=new Vector2(direction*speed,body.linearVelocity.y);
            // Once per lunge; targets the parked body as well as the active human.
            if(phase==Phase.Lunge && !lungeHit && Mathf.Abs(delta.x)<1 && Mathf.Abs(delta.y)<1.1f) {
                lungeHit=true; player.ReceiveDamage(20,body.position);
            }
            visual.flipX=direction>0; visual.color=flash>0?new Color(1,0.65f,0.65f):phase==Phase.Windup?new Color(1,0.2f,0.2f):Color.white;
            if(frames!=null&&frames.Length==8)visual.sprite=frames[flash>0?5:phase==Phase.Windup?3:phase==Phase.Lunge?4:phase==Phase.Recovery?6:1+(int)(pose*7)%2];
            visual.transform.localRotation=Quaternion.Euler(0,0,phase==Phase.Lunge?-direction*12:Mathf.Sin(pose*7)*1.5f);
        }
        public void TakeHit(int token,Vector2 source)
        {
            if(!CanAct || !player || !player.CanAct || !player.Rules.HitEnemy(token)) return;
            flash=0.16f; phase=Phase.Recovery; timer=0.5f;
            body.linearVelocity=new Vector2(body.position.x>=source.x?4:-4,3); HitConfirmed?.Invoke();
            if(player.Rules.EnemyDefeated) {
                capsule.enabled=false;body.simulated=false;
                if(frames!=null&&frames.Length==8){visual.sprite=frames[7];var ground=Physics2D.Raycast(body.position+Vector2.up,Vector2.down,5,LayerMask.GetMask("TerrainSolid"));if(ground){transform.position=new Vector3(body.position.x,ground.point.y,transform.position.z);body.position=transform.position;}}
                else visual.enabled=false;
                Defeated?.Invoke();
            }
        }
        void OnDisable() { if(body) body.simulated=false; }
    }
}
