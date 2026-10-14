using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VP515.Slice;

namespace VP515.Editor
{
    public static class SliceBuilder
    {
        public const string ScenePath="Assets/_VP515/Scenes/ChapterOneSlice.unity";
        const string Art="Assets/_VP515/Art/Slice/";
        static Transform worldRoot;
        static Sprite white;
        static Material glow;

        public static string Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before slice setup.");
            if(File.Exists(ScenePath))throw new InvalidOperationException("Slice exists; open it instead of overwriting.");
            var fonts=AssetDatabase.FindAssets("t:TMP_FontAsset");
            var font=fonts.Select(g=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(f=>f.name=="LiberationSans SDF")
                ??fonts.Select(g=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
            if(!font)throw new InvalidOperationException("Import TMP Essentials non-interactively before building.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("ChapterOneSlice");worldRoot=new GameObject("World").transform;worldRoot.SetParent(root.transform);
            var system=root.AddComponent<SliceBootstrap>();
            white=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            // White quad uses a shader-defined circular falloff; it is not painted game art.
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
            var savedTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"EffectWhite.asset");
            if(savedTexture){UnityEngine.Object.DestroyImmediate(texture);white=AssetDatabase.LoadAllAssetsAtPath(Art+"EffectWhite.asset").OfType<Sprite>().First();}
            else{AssetDatabase.CreateAsset(texture,Art+"EffectWhite.asset");white=UnityEngine.Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);white.name="EffectQuad";AssetDatabase.AddObjectToAsset(white,texture);}
            glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/_VP515/Materials/SliceGlow.mat");
            if(!glow){glow=new Material(Shader.Find("VP515/SoftGlow"));AssetDatabase.CreateAsset(glow,"Assets/_VP515/Materials/SliceGlow.mat");}

            var cameraGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraGo.tag="MainCamera";cameraGo.transform.SetParent(root.transform);
            var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.4f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.22f);
            camera.nearClipPlane=.1f;camera.farClipPlane=100;cameraGo.transform.position=new Vector3(9.6f,2.35f,-10);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var rig=cameraGo.AddComponent<SliceCamera>();system.CameraRig=rig;
            var lightGo=new GameObject("Warm Global Light");lightGo.transform.SetParent(root.transform);var light=lightGo.AddComponent<Light2D>();light.lightType=Light2D.LightType.Global;light.intensity=1;light.color=Color.white;
            var vista=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"ForestVista.png");
            {var sr=Paint("ForestVista",vista,new Vector2(4.5f,1.4f),44,-40,new Color(.95f,.96f,.98f,1));var p=sr.gameObject.AddComponent<SliceParallax>();p.FollowFactor=.85f;p.Initialize(camera);}
            var tree=Sprite("DeadTree");
            foreach(float x in new[]{-5f,7f,22f,39f,55f,71f}){var sr=Paint("FarTrunk",tree,new Vector2(x,-.5f),7,-25,new Color(.3f,.37f,.40f,.22f));var p=sr.gameObject.AddComponent<SliceParallax>();p.FollowFactor=.5f;p.Initialize(camera);}
            foreach(float x in new[]{-2f,17f,34f,61f}){var sr=Paint("NearTrunk",tree,new Vector2(x,-2),6,35,new Color(.07f,.10f,.11f,.9f));var p=sr.gameObject.AddComponent<SliceParallax>();p.FollowFactor=-.10f;p.Initialize(camera);}

            Ground(0,10.8f);Ground(13,25.5f);Ground(25.5f,60);
            Platform("SteppingRoot",new Vector2(8.1f,.55f),2.6f,.35f);
            Platform("LandingRock",new Vector2(15.8f,.8f),2.1f,.4f);
            foreach(float x in new[]{.3f,2.8f,6.4f,9.2f,13.2f,16.4f,19.5f,22.9f,28.5f,32.1f,35.5f,43.5f,47.7f,52.6f,58.4f})Paint("Grass",Sprite("GoldenGrass"),new Vector2(x,-.04f),1.15f,6,Color.white);
            foreach(float x in new[]{1.2f,18f,31.2f,48.8f,59.2f})Paint("WorldTree",tree,new Vector2(x,-.15f),3.6f,-5,new Color(.65f,.69f,.69f,1));
            var floorBounds=Box("LeftBoundary",new Vector2(-.5f,5),new Vector2(1,20));Box("RightBoundary",new Vector2(60.6f,5),new Vector2(1,20));
            Box("FlightCeiling",new Vector2(30,6.7f),new Vector2(64,.5f));
            var gate=Box("RootGateBarrier",new Vector2(26,1.8f),new Vector2(.85f,3.6f));
            var roof=Box("RootGateRoof",new Vector2(26,8.3f),new Vector2(.85f,7.4f));
            Paint("OverhangingRoots",tree,new Vector2(26.3f,4.6f),4,-3,new Color(.3f,.36f,.35f,1));
            var gateArt=Paint("GateFrame",Sprite("RuneGate"),new Vector2(26,.02f),3.7f,7,Color.white);
            var plate=Box("PressurePlate",new Vector2(21.5f,.06f),new Vector2(1.6f,.2f));plate.isTrigger=true;
            var plateArt=Paint("PlateGlow",white,new Vector2(21.5f,.04f),1.8f,8,new Color(.3f,.7f,.9f,.75f));plateArt.material=glow;plateArt.transform.localScale=new Vector3(1.8f,.25f,1);
            var sigil=Paint("UpperSigil",Sprite("SigilStone"),new Vector2(29,3.55f),1.35f,7,Color.white);
            Platform("SigilLedge",new Vector2(29,3.45f),3,.35f);
            var checkpoint=Paint("Checkpoint",Sprite("CheckpointLantern"),new Vector2(1.3f,0),1.1f,8,Color.white);
            var exit=Paint("ExitLantern",Sprite("CheckpointLantern"),new Vector2(56.5f,0),1.4f,8,Color.white);
            var dew=Paint("HealingDew",Sprite("Butterfly"),new Vector2(5.4f,.7f),.42f,12,new Color(.75f,1,1));
            var drift=dew.gameObject.AddComponent<SliceDrift>();drift.Range=new Vector2(.04f,.09f);drift.Speed=1.2f;
            foreach(var x in new[]{1.3f,26f,29f,56.5f})Glow("LanternGlow",new Vector2(x,x==29?4.1f:.9f),x==26?new Color(.2f,.8f,1,.25f):new Color(1,.7f,.32f,.38f),3,-1);
            for(int i=0;i<26;i++){
                float x=1+i*2.15f,y=.7f+Mathf.Sin(i*3.1f)*2;
                var mote=Glow("AmbientMote",new Vector2(x,y),new Color(.95f,.76f,.42f,.65f),.12f,30);
                var d=mote.gameObject.AddComponent<SliceDrift>();d.Range=new Vector2(.45f,.6f);d.Speed=.3f;d.Phase=i*.71f;
            }

            var playerGo=new GameObject("Player",typeof(SlicePlayer));playerGo.transform.SetParent(root.transform);playerGo.transform.position=new Vector3(3,.1f);playerGo.layer=LayerMask.NameToLayer("Player");
            var enemyGo=new GameObject("Thornling",typeof(SliceEnemy));enemyGo.transform.SetParent(root.transform);enemyGo.transform.position=new Vector3(40,.1f);enemyGo.layer=LayerMask.NameToLayer("Enemy");
            var world=root.AddComponent<SliceWorld>();world.PressurePlate=plate;world.Sigil=sigil.transform;world.Gate=gate;
            var uiGo=new GameObject("Interface",typeof(SliceUI));uiGo.transform.SetParent(root.transform);
            var audioGo=new GameObject("OriginalAudio",typeof(SliceAudio));audioGo.transform.SetParent(root.transform);
            system.Player=playerGo.GetComponent<SlicePlayer>();system.Enemy=enemyGo.GetComponent<SliceEnemy>();system.World=world;system.UI=uiGo.GetComponent<SliceUI>();system.Sound=audioGo.GetComponent<SliceAudio>();rig.Player=system.Player;
            system.Girl=Sprite("GirlIdle");system.Butterfly=Sprite("Butterfly");system.EnemyArt=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Thornling.png");system.Font=font;
            system.GirlFrames=new[]{"GirlIdle","GirlWalkA","GirlWalkB","GirlJump","GirlWindup","GirlSlash","GirlLand","GirlHurt"}.Select(Sprite).ToArray();
            system.Checkpoint=checkpoint.transform;system.Dew=dew.transform;system.Exit=exit.transform;system.GateVisual=gateArt;system.PlateVisual=plateArt;system.SigilVisual=sigil;
            system.GlowSprite=white;system.GlowMaterial=glow;
            PlayerSettings.runInBackground=true;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(ProjectPreparation.ScenePath,false)};
            return "Created ChapterOneSlice with original artwork, reusable world modules and runtime UI bindings";
        }
        static Sprite Sprite(string name)
        {
            string file=name.StartsWith("Girl")?"TravelerFrames.png":"SceneryAtlas.png";
            var sprite=AssetDatabase.LoadAllAssetsAtPath(Art+file).OfType<Sprite>().FirstOrDefault(s=>s.name==name);
            if(!sprite)throw new InvalidOperationException("Missing sprite: "+name);return sprite;
        }
        static SpriteRenderer Paint(string name,Sprite sprite,Vector2 pos,float width,int order,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(worldRoot);go.transform.position=pos;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;
            if(sprite)go.transform.localScale=Vector3.one*(width/Mathf.Max(.01f,sprite.bounds.size.x));return sr;
        }
        static BoxCollider2D Box(string name,Vector2 center,Vector2 size)
        {
            var go=new GameObject(name);go.transform.SetParent(worldRoot);go.transform.position=center;go.layer=LayerMask.NameToLayer("TerrainSolid");
            var box=go.AddComponent<BoxCollider2D>();box.size=size;return box;
        }
        static void Ground(float left,float right)
        {
            Box("GroundCollision",new Vector2((left+right)/2,-.3f),new Vector2(right-left,.6f));
            for(float x=left;x<right;){float width=Mathf.Min(8,right-x);Paint("RootRoad",Sprite("RootPlatform"),new Vector2(x+width/2,.12f),width,5,Color.white);x+=width;}
        }
        static void Platform(string name,Vector2 top,float width,float thickness)
        {
            Box(name+"Collision",top-Vector2.up*thickness/2,new Vector2(width,thickness));Paint(name,Sprite("RockPlatform"),top+Vector2.up*.05f,width,6,Color.white);
        }
        static SpriteRenderer Glow(string name,Vector2 p,Color color,float width,int order)
        {
            var sr=Paint(name,white,p,width,order,color);sr.material=glow;return sr;
        }
        public static string Validate()
        {
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            ValidateBindings(bootstrap);
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open the actual build target scene before validation.");
            RequireBank(bootstrap.GirlFrames,8,"hero poses");RequireBank(bootstrap.WalkFrames,8,"walk frames");RequireBank(bootstrap.EnemyFrames,8,"enemy frames");
            var environment=bootstrap.GetComponent<SliceEnvironment>();
            if(!environment||!environment.View||environment.View!=bootstrap.CameraRig.GetComponent<Camera>())throw new InvalidOperationException("Environment camera binding incomplete.");
            RequireBank(environment.Leaves,4,"foliage sprites");
            if(EditorUtility.scriptCompilationFailed)throw new InvalidOperationException("Compiler errors.");
            return "PASS: mandatory gameplay/presentation bindings, animation banks, radius and compiler";
        }
        internal static void ValidateBindings(SliceBootstrap b)
        {
            if(!b||!b.Player||!b.Enemy||!b.World||!b.UI||!b.Sound||!b.CameraRig||!b.Font||!b.Girl||!b.Butterfly||!b.EnemyArt||!b.Checkpoint||!b.Dew||!b.Exit||!b.GateVisual||!b.PlateVisual||!b.SigilVisual||!b.GlowSprite||!b.GlowMaterial)
                throw new InvalidOperationException("Mandatory slice bindings incomplete.");
            if(!b.CameraRig.GetComponent<Camera>()||b.CameraRig.Player!=b.Player||!b.World.PressurePlate||!b.World.Gate||!b.World.Sigil)throw new InvalidOperationException("Camera or puzzle bindings incomplete.");
            float radius=b.World.InteractionRadius;
            if(float.IsNaN(radius)||float.IsInfinity(radius)||radius<=0||radius>3)throw new InvalidOperationException("Interaction radius must be finite and in (0,3].");
        }
        internal static void RequireBank(Sprite[] bank,int count,string label)
        {
            if(bank==null||bank.Length!=count||bank.Any(s=>!s))throw new InvalidOperationException("Invalid "+label+": expected "+count+" non-null sprites.");
        }
        public static string BuildWindows()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before build.");
            Validate();
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the validated build scene first.");
            Directory.CreateDirectory("Builds/Windows/ChapterOneSlice");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/Windows/ChapterOneSlice/VP515.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Slice build failed: "+report.summary.result);
            return "Windows build succeeded; errors="+report.summary.totalErrors+", warnings="+report.summary.totalWarnings;
        }
    }
}
