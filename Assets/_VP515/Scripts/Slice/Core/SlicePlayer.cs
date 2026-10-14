using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VP515.Slice
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class SlicePlayer : MonoBehaviour
    {
        public SliceRules Rules { get; set; } = new SliceRules();
        public float Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public Vector2 BodyPosition { get { return body.position; } }
        public Transform BodyTransform { get { return transform; } }
        public Transform ShellTransform { get { return girl.transform; } }
        public SpriteRenderer BodyRenderer { get { return girl; } }
        public SpriteRenderer ButterflyRenderer { get { return wings; } }
        public Transform ActiveTransform { get { return Rules.Butterfly ? flight.transform : transform; } }
        public float AttackRemaining { get { return attackTime; } }
        public float HurtRemaining { get { return hurtTime; } }
        public Vector2 Motion { get { return input; } }
        public bool IsAttacking { get { return attackTime>0; } }
        public bool Alive { get { return Rules.Alive; } }
        public bool AnimatePose = true;
        public bool ShellOnPlate { get; set; }
        public Vector2 ActivePosition { get { return Rules.Butterfly ? flight.position : BodyPosition; } }
        public event Action<Vector2> AttackPerformed;
        public event Action<float> Damaged;
        public event Action FormChanged;
        public event Action InteractRequested;
        public bool CanAct { get { return canAct; } set { if(canAct==value) return; canAct=value; input=Vector2.zero; jumpBuffer=0; suppressInput=true; SyncPhysics(); } }
        Rigidbody2D body, flight; CapsuleCollider2D capsule; SpriteRenderer girl, wings;
        Sprite girlSprite, shellSprite; Vector2 input;
        float clock, coyote, jumpBuffer, cooldown, attackTime, poseTime, hurtTime;
        int attackToken; bool canAct=true, wasButterfly, suppressInput;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        void Awake()
        {
            body=GetComponent<Rigidbody2D>(); capsule=GetComponent<CapsuleCollider2D>();
            body.freezeRotation=true; body.gravityScale=3; body.interpolation=RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            capsule.size=new Vector2(0.65f,1.6f); capsule.offset=new Vector2(0,0.85f);
            girl=new GameObject("BodySprite").AddComponent<SpriteRenderer>(); girl.transform.SetParent(transform,false);
            var root=new GameObject("ButterflyRoot"); root.layer=gameObject.layer;
            wings=new GameObject("ButterflySprite").AddComponent<SpriteRenderer>(); wings.transform.SetParent(root.transform,false);
            flight=root.AddComponent<Rigidbody2D>(); flight.gravityScale=0; flight.freezeRotation=true;
            flight.interpolation=RigidbodyInterpolation2D.Interpolate; flight.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var circle=root.AddComponent<CircleCollider2D>(); circle.radius=0.18f;
            Physics2D.IgnoreCollision(capsule,circle); root.SetActive(false);
        }
        public void Initialize(SliceRules rules,Sprite girlArt,Sprite butterfly,Sprite shell)
        {
            Rules=rules ?? new SliceRules(); girlSprite=girlArt; shellSprite=shell ? shell : girlArt;
            girl.sprite=girlArt; wings.sprite=butterfly; girl.sortingOrder=wings.sortingOrder=10;
            // Injected art owns PPU/pivot (including transparent atlas padding).
            girl.transform.localPosition=Vector3.zero; girl.transform.localScale=Vector3.one;
            if(butterfly) wings.transform.localScale=Vector3.one*(0.55f/Mathf.Max(0.01f,butterfly.bounds.size.x));
            wasButterfly=false; SyncForm(); SyncPhysics();
        }
        public void ResetAt(Vector2 pos)
        {
            Rules.Respawn(); transform.position=new Vector3(pos.x,pos.y,transform.position.z);body.position=pos;body.linearVelocity=Vector2.zero;
            flight.transform.position=new Vector3(pos.x,pos.y+1,flight.transform.position.z);flight.position=pos+Vector2.up;
            flight.linearVelocity=Vector2.zero; clock=coyote=jumpBuffer=cooldown=attackTime=poseTime=hurtTime=0;
            input=Vector2.zero; ShellOnPlate=false; IsGrounded=false; Facing=1; suppressInput=true; SyncForm(); SyncPhysics();
            Physics2D.SyncTransforms();
        }
        void Update()
        {
            SyncForm(); SyncPhysics();
            if(!CanAct || !Rules.Alive) { input=Vector2.zero; return; }
            float dt=Time.deltaTime; clock+=dt; cooldown=Mathf.Max(0,cooldown-dt);
            attackTime=Mathf.Max(0,attackTime-dt); hurtTime=Mathf.Max(0,hurtTime-dt); poseTime+=dt;
            Rules.Tick(dt,true); SyncForm(); SyncPhysics();
            jumpBuffer=Mathf.Max(0,jumpBuffer-dt);
            coyote=IsGrounded ? 0.12f : Mathf.Max(0,coyote-dt);
            if(suppressInput) { suppressInput=false; return; }
            input=new Vector2((Held("right")?1:0)-(Held("left")?1:0),(Held("up")?1:0)-(Held("down")?1:0));
            if(Mathf.Abs(input.x)>0.01f) Facing=Mathf.Sign(input.x);
            if(Pressed("jump") && !Rules.Butterfly) jumpBuffer=0.12f;
            if(Pressed("form") && Rules.ToggleForm()) SyncForm();
            if(Pressed("attack") && !Rules.Butterfly && cooldown<=0) {
                attackToken=Rules.BeginAttack(); if(attackToken>0) { cooldown=0.38f; attackTime=0.18f; AttackPerformed?.Invoke(BodyPosition+Vector2.up*0.9f); }
            }
            if(Pressed("interact")) InteractRequested?.Invoke();
        }
        // Optional external input adapter; Update reads the keyboard by default.
        public void Move(Vector2 direction,bool jump=false) { if(!CanAct || !Rules.Alive) return; input=Vector2.ClampMagnitude(direction,1); if(jump) jumpBuffer=0.12f; }
        void FixedUpdate()
        {
            SyncPhysics(); if(!CanAct || !Rules.Alive) return;
            if(Mathf.Abs(input.x)>0.01f) Facing=Mathf.Sign(input.x);
            if(Rules.Butterfly) { flight.linearVelocity=Vector2.ClampMagnitude(input,1)*4; IsGrounded=false; return; }
            var filter=new ContactFilter2D(); filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer)); filter.useTriggers=false;
            int count=capsule.Cast(Vector2.down,filter,groundHits,0.07f); IsGrounded=false;
            for(int i=0;i<count;i++) if(body.linearVelocity.y<=0.1f && groundHits[i].normal.y>0.5f && groundHits[i].rigidbody!=body) IsGrounded=true;
            if(IsGrounded && body.linearVelocity.y<=0) coyote=0.12f;
            float vx=Mathf.MoveTowards(body.linearVelocity.x,input.x*4.6f,(IsGrounded?45:25)*Time.fixedDeltaTime);
            body.linearVelocity=new Vector2(vx,body.linearVelocity.y);
            if(jumpBuffer>0 && coyote>0) { body.linearVelocity=new Vector2(vx,10); jumpBuffer=coyote=0; IsGrounded=false; }
            if(attackTime>0) foreach(var hit in Physics2D.OverlapCircleAll(BodyPosition+new Vector2(Facing*0.75f,0.9f),0.7f)) {
                var enemy=hit.GetComponentInParent<SliceEnemy>(); if(enemy) enemy.TakeHit(attackToken,BodyPosition);
            }
        }
        void SyncPhysics()
        {
            if(!body || !flight) return;
            bool bodyActive=isActiveAndEnabled && canAct && Rules.Alive && !Rules.Butterfly;
            bool flightActive=isActiveAndEnabled && canAct && Rules.Alive && Rules.Butterfly;
            // Reassigning simulated every render frame invalidates interpolated poses in Unity 6.
            // Only change physics participation on an actual form/pause/death transition.
            if(body.simulated!=bodyActive)body.simulated=bodyActive;
            if(flight.simulated!=flightActive)flight.simulated=flightActive;
        }
        void SyncForm()
        {
            if(!girl || wasButterfly==Rules.Butterfly) return;
            wasButterfly=Rules.Butterfly; attackTime=jumpBuffer=coyote=0;
            if(wasButterfly) { body.linearVelocity=Vector2.zero; flight.position=BodyPosition+Vector2.up*1.1f; flight.linearVelocity=Vector2.zero; }
            girl.sprite=wasButterfly ? shellSprite : girlSprite;
            wings.transform.parent.gameObject.SetActive(wasButterfly); SyncPhysics(); FormChanged?.Invoke();
        }
        public void ReceiveDamage(float amount,Vector2 source)
        {
            if(!CanAct || !Rules.Damage(amount,clock)) return;
            hurtTime=0.2f; attackTime=0; SyncForm(); SyncPhysics();
            if(Rules.Alive && !Rules.Butterfly) body.linearVelocity=new Vector2(BodyPosition.x>=source.x?4:-4,4);
            Damaged?.Invoke(amount);
        }
        void LateUpdate()
        {
            if(!girl || !CanAct || !AnimatePose) return;
            // Single-sprite pose animation, not a rig/frame bank. Keep the bottom at the feet.
            float lean=Rules.Butterfly?0:(!IsGrounded ? -Facing*5 : -input.x*3);
            if(attackTime>0) lean=-Facing*12;
            girl.transform.localRotation=Quaternion.Euler(0,0,lean);
            float squash=IsGrounded && Mathf.Abs(input.x)>0.1f && !Rules.Butterfly ? 1+Mathf.Sin(poseTime*18)*0.012f : 1;
            girl.transform.localScale=new Vector3(1/squash,squash,1);
            girl.flipX=Facing<0; girl.color=hurtTime>0?new Color(1,0.45f,0.45f):Rules.Butterfly?new Color(0.6f,0.65f,0.65f):Color.white;
            wings.flipX=Facing<0; wings.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(poseTime*16)*8);
        }
        void OnDisable() { if(body) body.simulated=false; if(flight) { flight.simulated=false; flight.gameObject.SetActive(false); } }
        void OnEnable() { if(flight) { flight.gameObject.SetActive(Rules.Butterfly); SyncPhysics(); } }
        void OnDestroy() { if(flight) Destroy(flight.gameObject); }
        static bool Held(string key)
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current; if(k==null) return false;
            switch(key) { case "left":return k.aKey.isPressed||k.leftArrowKey.isPressed; case "right":return k.dKey.isPressed||k.rightArrowKey.isPressed; case "up":return k.wKey.isPressed||k.upArrowKey.isPressed; case "down":return k.sKey.isPressed||k.downArrowKey.isPressed; }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch(key) { case "left":return Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow); case "right":return Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow); case "up":return Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow); case "down":return Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow); }
#endif
            return false;
        }
        static bool Pressed(string key)
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;
            switch(key) { case "jump":return k!=null&&k.spaceKey.wasPressedThisFrame; case "form":return k!=null&&k.qKey.wasPressedThisFrame; case "interact":return k!=null&&k.eKey.wasPressedThisFrame; case "attack":return (k!=null&&k.jKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame&&!PointerOnUI(Mouse.current.position.ReadValue())); }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch(key) { case "jump":return Input.GetKeyDown(KeyCode.Space); case "form":return Input.GetKeyDown(KeyCode.Q); case "interact":return Input.GetKeyDown(KeyCode.E); case "attack":return Input.GetKeyDown(KeyCode.J)||(Input.GetMouseButtonDown(0)&&!PointerOnUI(Input.mousePosition)); }
#endif
            return false;
        }
        static bool PointerOnUI(Vector2 position)
        {
            if(!EventSystem.current)return false;
            // Query this press position immediately; cached pointer-over state can be one frame old.
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            foreach(var hit in hits)if(hit.gameObject.GetComponentInParent<Canvas>())return true;
            return false;
        }
    }
}
