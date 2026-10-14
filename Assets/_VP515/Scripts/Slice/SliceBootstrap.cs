using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VP515.Slice
{
    public sealed class SliceBootstrap : MonoBehaviour
    {
        public SlicePlayer Player;
        public SliceEnemy Enemy;
        public SliceWorld World;
        public SliceUI UI;
        public SliceAudio Sound;
        public SliceCamera CameraRig;
        public TMP_FontAsset Font;
        public Sprite[] GirlFrames;
        public Sprite[] WalkFrames;
        public Sprite[] EnemyFrames;
        public Sprite Girl,Butterfly,EnemyArt,GlowSprite;
        public Material GlowMaterial;
        public Transform Checkpoint,Dew,Exit;
        public SpriteRenderer GateVisual,PlateVisual,SigilVisual;
        public SliceRules Rules { get; private set; }
        public float Elapsed { get; private set; }
        public int Collected { get; private set; }
        bool journey,helpPlate,helpAttack;float suppressUntil,lastHealth=100,hitPauseUntil;
        Vector2 spawn=new Vector2(3,.1f);
        UnityEngine.UI.Image fade;float fadeAmount=1;

        void Start()
        {
            Application.runInBackground=true;
            Rules=new SliceRules();Rules.Reset();
            Sound.Initialize();
            Player.Initialize(Rules,Girl,Butterfly,Girl);Player.ResetAt(spawn);
            Enemy.Initialize(Player,EnemyArt,37,44);World.Initialize(Player,Rules);
            Enemy.SetFrames(EnemyFrames);
            var trail=Player.ButterflyRenderer.transform.parent.gameObject.AddComponent<TrailRenderer>();trail.time=.38f;trail.minVertexDistance=.035f;trail.startWidth=.055f;trail.endWidth=0;trail.material=GlowMaterial;trail.startColor=new Color(.3f,.95f,1,.65f);trail.endColor=new Color(.25f,.7f,1,0);trail.sortingOrder=11;
            Player.FormChanged+=()=>{trail.Clear();Sound.Chime();Burst(Player.BodyPosition+Vector2.up,new Color(.25f,.85f,1),12);};
            var enemyHealth=Enemy.gameObject.AddComponent<SliceEnemyHealth>();enemyHealth.Rules=Rules;enemyHealth.Quad=GlowSprite;enemyHealth.Glow=GlowMaterial;
            var animator=Player.gameObject.AddComponent<SliceFrameAnimator>();animator.Player=Player;animator.Frames=GirlFrames;animator.WalkFrames=WalkFrames;animator.Sound=Sound;
            Player.InteractRequested+=Interact;Player.AttackPerformed+=Swing;Player.Damaged+=Damage;
            Enemy.HitConfirmed+=Hit;Enemy.Defeated+=EnemyDown;World.GateOpened+=GateOpened;
            UI.Build(Font);
            foreach(var canvas in UI.GetComponentsInChildren<Canvas>(true)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=CameraRig.GetComponent<Camera>();canvas.planeDistance=1;}
            UI.StartRequested=Begin;UI.RestartRequested=()=>{if(UI.CurrentView==SliceView.Dead)Respawn();else Begin();};UI.ResumeRequested=()=>UI.Show(SliceView.Playing);
            UI.MenuRequested=Menu;UI.QuitRequested=()=>{ Application.Quit(); };
            UI.DewRequested=()=>{if(Rules.UseDew()){Sound.Chime();UI.ShowToast("Dew restored 35 health.");}else UI.ShowToast("Use dew after taking damage.");};
            UI.VolumeChanged=(v)=>{AudioListener.volume=v;PlayerPrefs.SetFloat("vp515.volume",v);PlayerPrefs.Save();};
            UI.ShakeChanged=(v)=>{CameraRig.ShakeEnabled=v;PlayerPrefs.SetInt("vp515.shake",v?1:0);PlayerPrefs.Save();};
            UI.ViewChanged=ChangeView;
            float volume=Mathf.Clamp01(PlayerPrefs.GetFloat("vp515.volume",.8f));bool shake=PlayerPrefs.GetInt("vp515.shake",1)==1;
            AudioListener.volume=volume;CameraRig.ShakeEnabled=shake;UI.SetSettings(volume,shake);
            CreateFade();Menu();
        }
        void CreateFade()
        {
            var go=new GameObject("TransitionVeil",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster));
            go.transform.SetParent(transform,false);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=CameraRig.GetComponent<Camera>();canvas.planeDistance=.5f;
            var imageGo=new GameObject("Fade",typeof(RectTransform),typeof(UnityEngine.UI.Image));imageGo.transform.SetParent(go.transform,false);
            var rect=imageGo.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            fade=imageGo.GetComponent<UnityEngine.UI.Image>();fade.raycastTarget=false;fade.color=Color.black;
        }
        public void Begin()
        {
            hitPauseUntil=0;Rules.Reset();World.ResetWorld();Player.ResetAt(spawn);Enemy.ResetEnemy();
            journey=true;Elapsed=0;Collected=0;helpPlate=helpAttack=false;lastHealth=100;
            CameraRig.ResetView();CameraRig.Following=true;fadeAmount=1;
            UI.Show(SliceView.Playing);UI.ShowToast("A / D to move     SPACE to jump     E to interact");Sound.Chime();
        }
        void Menu()
        {
            journey=false;hitPauseUntil=0;CameraRig.Following=false;CameraRig.ResetView();
            UI.Show(SliceView.Menu);fadeAmount=.65f;
        }
        void ChangeView(SliceView view)
        {
            bool active=view==SliceView.Playing;
            Player.CanAct=active&&Rules.Alive;Enemy.CanAct=active&&Rules.Alive;
            Time.timeScale=active?1:0;suppressUntil=Time.unscaledTime+.2f;
            if(active)StartCoroutine(SuppressClosingClick());
        }
        IEnumerator SuppressClosingClick()
        {
            Player.CanAct=false;Enemy.CanAct=false;
            yield return new WaitForSecondsRealtime(.2f);
            if(UI.CurrentView==SliceView.Playing&&Rules.Alive){Player.CanAct=true;Enemy.CanAct=true;}
        }
        void Update()
        {
            if(Rules==null)return;
            fadeAmount=Mathf.MoveTowards(fadeAmount,0,Time.unscaledDeltaTime*1.7f);if(fade)fade.color=new Color(.015f,.025f,.03f,fadeAmount);
            if(Keyboard.current!=null&&Time.unscaledTime>suppressUntil)
            {
                if(Keyboard.current.escapeKey.wasPressedThisFrame){
                    if(UI.CurrentView==SliceView.Playing)UI.Show(SliceView.Paused);
                    else if(UI.CurrentView==SliceView.Paused||UI.CurrentView==SliceView.Inventory)UI.Show(SliceView.Playing);
                    else if(UI.CurrentView==SliceView.Controls||UI.CurrentView==SliceView.Settings)UI.Back();
                }
                if(Keyboard.current.tabKey.wasPressedThisFrame){
                    if(UI.CurrentView==SliceView.Playing)UI.Show(SliceView.Inventory);
                    else if(UI.CurrentView==SliceView.Inventory)UI.Show(SliceView.Playing);
                }
            }
            bool playing=UI.CurrentView==SliceView.Playing;
            if(playing)
            {
                Elapsed+=Time.deltaTime;
                if(hitPauseUntil>0&&Time.unscaledTime>=hitPauseUntil){hitPauseUntil=0;Time.timeScale=1;}
                if(!Rules.Alive){UI.Show(SliceView.Dead);fadeAmount=.5f;}
                else if(Player.ActivePosition.y< -5||Player.ActivePosition.x< -.8f||Player.ActivePosition.x>61){Respawn();}
                if(Player.BodyPosition.x>32&&!helpAttack){helpAttack=true;UI.ShowToast("J / LEFT CLICK: strike     Wait for the creature's opening.");}
                if(Player.BodyPosition.x>18&&!helpPlate){helpPlate=true;UI.ShowToast("Stand on the glowing plate. Q leaves your body; fly through the upper gap.");}
            }
            if(GateVisual)GateVisual.color=Color.Lerp(GateVisual.color,Rules.GateUnlocked?new Color(.6f,.9f,1,.2f):Color.white,Time.unscaledDeltaTime*3);
            if(PlateVisual)PlateVisual.color=Player.ShellOnPlate?new Color(.4f,1,1):new Color(.5f,.6f,.65f);
            if(SigilVisual)SigilVisual.color=Rules.GateUnlocked?new Color(.5f,1,1):Color.white;
            string objective=!Rules.GateUnlocked?"OPEN THE ROOT GATE":!Rules.EnemyDefeated?"CLEAR THE PATH":"REACH THE EXIT LANTERN";
            string prompt=Prompt();
            UI.UpdateHUD(new SliceHudData {Health=Rules.Health,Energy=Rules.Energy,DewCount=Rules.DewCount,Elapsed=Elapsed,
                Objective=objective,Prompt=prompt,Butterfly=Rules.Butterfly,GateUnlocked=Rules.GateUnlocked,EnemyDefeated=Rules.EnemyDefeated});
            lastHealth=Rules.Health;
        }
        string Prompt()
        {
            Vector2 p=Player.ActivePosition;
            if(Rules.Butterfly){
                if(Vector2.Distance(p,World.Sigil.position)<=World.InteractionRadius)return Player.ShellOnPlate?"E  ·  Awaken the sigil":"Your body must hold the pressure plate.";
                return "W A S D  ·  Fly       Q  ·  Return to your body";
            }
            if(Dew&&Dew.gameObject.activeSelf&&Vector2.Distance(p,Dew.position)<=World.InteractionRadius)return "E  ·  Gather healing dew";
            if(Vector2.Distance(p,Exit.position)<1.5f)return Rules.EnemyDefeated&&Rules.GateUnlocked?"E  ·  Finish this section":"Clear the creature and open the gate first.";
            if(Vector2.Distance(p,Checkpoint.position)<1.7f)return "E  ·  Rest at the lantern";
            if(p.x>19&&p.x<24&&!Rules.GateUnlocked)return "Q  ·  Leave your body on the plate";
            return "";
        }
        void Interact()
        {
            if(UI.CurrentView!=SliceView.Playing)return;
            if(Rules.Butterfly){
                if(Vector2.Distance(Player.ActivePosition,World.Sigil.position)<=World.InteractionRadius&&!World.TryActivateSigil())UI.ShowToast("The plate needs the weight of your body.");
                return;
            }
            if(Dew&&World.TryCollectDew(Dew.gameObject)){Collected++;Sound.Chime();Burst(Dew.position,new Color(.35f,.95f,1),8);UI.ShowToast("Healing dew collected. TAB opens your satchel.");return;}
            if(Vector2.Distance(Player.BodyPosition,Checkpoint.position)<1.7f){spawn=new Vector2(Checkpoint.position.x+1,.1f);Rules.Respawn();Player.ResetAt(spawn);Sound.Chime();UI.ShowToast("Restored. This lantern is your checkpoint.");return;}
            if(Vector2.Distance(Player.BodyPosition,Exit.position)<1.5f){
                if(Rules.GateUnlocked&&Rules.EnemyDefeated){UI.SetSummary(Elapsed,Rules.EnemyDefeated,Collected);UI.Show(SliceView.Complete);Sound.Chime();}
                else UI.ShowToast("The path is not yet clear.");
            }
        }
        public void Respawn()
        {
            hitPauseUntil=0;Time.timeScale=1;Player.ResetAt(spawn);Enemy.ResetEnemy();UI.Show(SliceView.Playing);
            fadeAmount=.8f;Sound.Chime();UI.ShowToast("Returned to your checkpoint. The awakened gate remains open.");
        }
        void Swing(Vector2 position)
        {
            Sound.Swing();var go=new GameObject("BladeArc");go.transform.position=position;
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=14;
            line.material=GlowMaterial;line.startColor=new Color(.55f,1,1,.85f);line.endColor=new Color(.95f,.85f,.55f,.03f);line.startWidth=.10f;line.endWidth=.015f;line.sortingOrder=45;
            for(int i=0;i<14;i++){float a=Mathf.Lerp(-.9f,.9f,i/13f);line.SetPosition(i,new Vector3(Mathf.Cos(a)*1.3f*Player.Facing,Mathf.Sin(a)*1.3f,0));}
            StartCoroutine(FadeArc(go,line));
        }
        IEnumerator FadeArc(GameObject go,LineRenderer line)
        {
            float t=0;while(t<.16f){t+=Time.deltaTime;if(line){line.startWidth=.1f*(1-t/.16f);}yield return null;}if(go)Destroy(go);
        }
        void Hit(){Sound.Hit();CameraRig.Impact(.035f);Burst(Enemy.transform.position+Vector3.up*.7f,new Color(.9f,.8f,.5f),12);hitPauseUntil=Time.unscaledTime+.05f;if(UI.CurrentView==SliceView.Playing)Time.timeScale=.08f;}
        void Damage(float amount){Sound.Hit();CameraRig.Impact(.07f);Burst(Player.BodyPosition+Vector2.up, new Color(1,.4f,.25f),5);}
        void EnemyDown(){Sound.Chime();UI.ShowToast("The path is clear. Follow the exit lantern.");}
        void GateOpened(){Sound.Chime();Burst(World.Sigil.position,new Color(.2f,.95f,1),18);UI.ShowToast("Gate awakened. Q returns you to your body.");}
        void Burst(Vector3 position,Color color,int count)
        {
            for(int i=0;i<count;i++){
                var go=new GameObject("LightMote");go.transform.position=position+new Vector3(Random.Range(-.5f,.5f),Random.Range(-.2f,.6f));
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=GlowSprite;sr.material=GlowMaterial;sr.color=color;sr.sortingOrder=48;go.transform.localScale=Vector3.one*Random.Range(.12f,.28f);
                var drift=go.AddComponent<SliceDrift>();drift.Range=new Vector2(.5f,1);drift.Speed=3;drift.Phase=i;
                var fadeOut=go.AddComponent<SliceFade>();fadeOut.Visual=sr;fadeOut.Duration=.55f;
            }
        }
        void OnDestroy(){Time.timeScale=1;if(Player){Player.InteractRequested-=Interact;Player.AttackPerformed-=Swing;Player.Damaged-=Damage;}if(Enemy){Enemy.HitConfirmed-=Hit;Enemy.Defeated-=EnemyDown;}if(World)World.GateOpened-=GateOpened;}
    }
}
