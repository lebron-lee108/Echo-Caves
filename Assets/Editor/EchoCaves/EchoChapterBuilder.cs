using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoCaves.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static EchoCaves.Editor.EchoEnvironmentArt;

namespace EchoCaves.Editor
{
    public static class EchoChapterBuilder
    {
        static readonly Color Gold=new Color(.63f,.39f,.16f), Warm=new Color(.94f,.64f,.24f), Cyan=new Color(.2f,.78f,.88f), Dark=new Color(.025f,.055f,.09f);
        static readonly List<EchoAnchor> Anchors=new List<EchoAnchor>();
        static System.Random rng;
        static Camera camera;
        static Transform env;
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before authoring");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Existing scene dirty: no automatic save/overwrite");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(EchoSceneChecks.ScenePath))throw new Exception("Output exists: refuse destructive rebuild. Polish in place.");
            if(!(GraphicsSettings.currentRenderPipeline?.GetType().FullName??"").Contains("Universal"))throw new Exception("Expected URP");
            Prepare();rng=new System.Random(51519);Anchors.Clear();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=Node("Echo Caves — Chapter 01 · C handoff",null);env=root.transform;
            var cg=Node("Main Camera",env);cg.tag="MainCamera";camera=cg.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=13.5f;camera.transform.position=new Vector3(68,9,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Dark;camera.allowHDR=false;camera.allowMSAA=false;camera.allowDynamicResolution=false;cg.AddComponent<AudioListener>();
            var pp=cg.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();pp.assetsPPU=16;pp.refResolutionX=768;pp.refResolutionY=432;pp.gridSnapping=UnityEngine.Rendering.Universal.PixelPerfectCamera.GridSnapping.PixelSnapping;pp.cropFrame=UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.None;
            var review=cg.AddComponent<EchoSceneReview>();review.ReviewCamera=camera;
            review.Bookmarks=new[]{new Vector2(-26,37),new Vector2(-5,36),new Vector2(23,6),new Vector2(70,9),new Vector2(119,9),new Vector2(167,3),new Vector2(198,-17),new Vector2(184,-30),new Vector2(123,-37),new Vector2(45,-38)};
            review.BookmarkNames=new[]{"洞外经营","洞内主井","A灯尾蛾与首跳","B菌菇绳桥","C观察高廊","D引兽越桥","D-E下行洞道","E净化维护道","F非致命遭遇","G旧站台返程"};review.CurrentBookmark=3;
            Backgrounds();
            Surface();A();B();C();D();Descent();E();F();G();Shaft();
            EchoUIBuilder.Build(env);
            EditorSceneManager.SaveScene(scene,EchoSceneChecks.ScenePath);
            WritePoints();
            return EchoSceneChecks.Validate();
        }
        static Transform Region(string id){return Node("Region_"+id,env).transform;}
        static void Anchor(string id,string r,float x,float y,string note)
        {var g=Node("Anchor_"+id,env,x,y);var a=g.AddComponent<EchoAnchor>();a.Id=id;a.Region=r;a.Handoff=note;Anchors.Add(a);}
        static void Backgrounds()
        {
            foreach(var row in new[]{new[]{4f,34f},new[]{-37f,38f}})
            {
                foreach(var tuple in new[]{new[]{"FarLayer","FarCavern","0.18"},new[]{"MidLayer","MidCavern","0.46"},new[]{"NearLayer","MidCavern","1.10"}})
                {
                    string name=tuple[0];var g=Node(name,env);var p=g.AddComponent<EchoParallaxLayer>();p.TargetCamera=camera;p.RelativeSpeed=float.Parse(tuple[2],System.Globalization.CultureInfo.InvariantCulture);p.ReferenceCamera=camera.transform.position;p.ReferencePosition=g.transform.position;
                    int order=name=="FarLayer"?-100:name=="MidLayer"?-65:40;
                    for(int j=-3;j<7;j++)
                    {
                        float x=j*70f+35;var s=Art(tuple[1],g.transform,x,row[0],72,row[1],order,name=="NearLayer"?new Color(.15f,.18f,.23f,.48f):Color.white);
                        if(name=="NearLayer")s.transform.localScale=new Vector3(s.transform.localScale.x,s.transform.localScale.y*1.28f,1);
                    }
                }
            }
        }
        static void Ground(Transform p,string name,float x0,float x1,float y,float depth=6,float rugged=.35f,bool adorn=true)
        {
            int n=Mathf.Max(2,Mathf.CeilToInt((x1-x0)/1.5f));var pts=new List<Vector2>();
            float last=y;
            for(int i=0;i<=n;i++)
            {
                float x=Mathf.Lerp(x0,x1,i/(float)n);float h=i==0||i==n?y:y+Mathf.Round(((float)rng.NextDouble()-.5f)*rugged*16)/16;
                if(i>0){pts.Add(new Vector2(x,last));pts.Add(new Vector2(x,h));}else pts.Add(new Vector2(x,h));last=h;
            }
            Rock(name,p,pts.ToArray(),depth);
            for(float x=x0+.5f;x<x1-.4f;x+=2.0f)
            {
                Art("MossLip",p,x,y+.10f,2.12f,.48f,16,new Color(.75f,.84f,.8f));
                if(adorn&&rng.NextDouble()<.35)Art("Fern",p,x,y+.35f,1.2f,.7f,18);
                if(adorn&&rng.NextDouble()<.23)Art("Rubble",p,x,y+.22f,1,.6f,17);
            }
        }
        static void Roof(Transform p,string name,float x0,float x1,float y,float variation=1.4f)
        {
            var pts=new List<Vector2>();int n=Mathf.CeilToInt((x1-x0)/2f);
            for(int i=0;i<=n;i++)pts.Add(new Vector2(Mathf.Lerp(x0,x1,i/(float)n),y-(float)rng.NextDouble()*variation));
            Rock(name,p,pts.ToArray(),5,22,true);
            for(float x=x0+2;x<x1-1;x+=4.3f){Art("Stalactite",p,x,y-1.3f,3,3.5f,24);if(rng.NextDouble()<.45)Art("Vine",p,x+1,y-2.2f,.7f,3.5f,25);}
        }
        static void Lamp(Transform p,float x,float y)
        {
            Art("Lantern",p,x,y,1.1f,1.5f,26);
            Pixel("LampGlow",p,x,y-.25f,1.35f,1.4f,new Color(1,.6f,.15f,.06f),23);
        }
        static void Fungi(Transform p,float x,float y,float size=1.5f)
        {Art("Mushroom",p,x,y+size*.38f,size,size,21);}
        static void Ladder(Transform p,string id,float x,float bottom,float top)
        {
            var g=Node("Ladder_"+id,p);for(float y=bottom;y<=top;y+=.5f)Pixel("Rung",g.transform,x,y,.88f,.12f,Warm,24);
            Pixel("LeftRail",g.transform,x-.4f,(bottom+top)/2,.13f,top-bottom+.4f,Gold,23);Pixel("RightRail",g.transform,x+.4f,(bottom+top)/2,.13f,top-bottom+.4f,Gold,23);
            var trigger=g.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.offset=new Vector2(x,(bottom+top)/2);trigger.size=new Vector2(1.1f,top-bottom+.7f);
            var a=g.AddComponent<EchoAnchor>();a.Id="Ladder_"+id;a.Handoff="B接攀爬；此组件只有几何与触发范围";a.Region=id.Split('_')[0];
        }
        static void Wood(Transform p,string name,float x0,float x1,float y,bool supports=true)
        {
            var g=Node(name,p);float w=x1-x0;
            Pixel("Deck",g.transform,(x0+x1)/2,y-.15f,w,.3f,new Color(.29f,.17f,.10f),17);
            for(float x=x0+.22f;x<x1;x+=.55f){Pixel("Plank",g.transform,x,y,.49f,.16f,new Color(.48f,.29f,.14f),20);Pixel("Bolt",g.transform,x,y+.04f,.0625f,.0625f,Warm,21);}
            var c=g.AddComponent<BoxCollider2D>();c.offset=new Vector2((x0+x1)/2,y-.15f);c.size=new Vector2(w,.3f);
            for(float x=x0;x<=x1;x+=3.5f){Pixel("RailPost",g.transform,x,y+.6f,.18f,1.6f,Gold,19);if(supports){Pixel("Support",g.transform,x,y-2,.3f,4,Gold*.65f,12);Line("Brace",g.transform,new Vector2(x,y-2),new Vector2(x+2,y-.3f),.2f,Gold*.65f,13);}}
            Line("Handrail",g.transform,new Vector2(x0,y+1),new Vector2(x1,y+1),.12f,Gold,18);
        }
        static void Bridge(Transform p,string id,float x0,float x1,float y,float sag)
        {
            var g=Node("RopeBridge_"+id,p);int n=Mathf.CeilToInt((x1-x0)/.6f);var edge=new List<Vector2>();
            Func<float,float> height=t=>y-sag*Mathf.Sin(t*Mathf.PI);
            for(int i=0;i<n;i++)
            {
                float t0=i/(float)n,t1=(i+1f)/n;var a=new Vector2(Mathf.Lerp(x0,x1,t0),height(t0));var b=new Vector2(Mathf.Lerp(x0,x1,t1),height(t1));edge.Add(a);
                Line("BridgePlank",g.transform,a,b,.23f,new Color(.55f,.34f,.16f),22);
                Line("SuspensionRope",g.transform,a+Vector2.up*1.2f,b+Vector2.up*1.2f,.10f,Warm*.78f,23);
                if(i%2==0)Line("RopeTie",g.transform,a,a+Vector2.up*1.2f,.07f,Gold,24);
            }
            edge.Add(new Vector2(x1,y));g.AddComponent<EdgeCollider2D>().points=edge.ToArray();
            foreach(float x in new[]{x0,x1}){Pixel("AnchorPost",g.transform,x,y+.8f,.3f,2.3f,Gold,25);Line("RockAnchor",g.transform,new Vector2(x,y+.6f),new Vector2(x+(x==x0?-.8f:.8f),y),.15f,Gold,24);}
            Anchor(id+"_RopeBridge",id,(x0+x1)/2,y,"静态绳桥：B接普通通行，无断裂摆荡机制");
        }
        static void Device(Transform p,string id,string key,float x,float y,float w=2,float h=2,string[] states=null)
        {
            var g=Node("Device_"+id,p,x,y+h*.5f);var vs=g.AddComponent<EchoVisualState>();vs.StateNames=states??new[]{"未操作","可操作","已完成"};vs.StateArt=new GameObject[vs.StateNames.Length];
            for(int i=0;i<vs.StateArt.Length;i++){var state=Node(vs.StateNames[i],g.transform);Art(key,state.transform,0,0,w,h,27);Pixel("StateIndicator",state.transform,w*.32f,h*.28f,.15f,.18f,i==0?new Color(.6f,.17f,.1f):i==1?Warm:Cyan,29);state.SetActive(i==0);vs.StateArt[i]=state;}
            Anchor(id,p.name.Replace("Region_",""),x,y,"C视觉状态，B接入交互与逻辑；资源待美术确认");
        }
        static void Surface()
        {
            var p=Region("Surface");Art("OutsideBackdrop",p,-31,38,72,30,-90);Ground(p,"Surface_Outside",-50,-12,30,7,.2f);Ground(p,"Surface_MapLanding",-12,-2,30,6,.1f);
            Art("RadioHut",p,-36,34.6f,15,10,17);Art("ToolShed",p,-21,33.6f,12,8,18);Lamp(p,-29,32.5f);
            Roof(p,"Surface_CaveRoof",-12,6,40,1.1f);
            Device(p,"Surface_Map","Sign",-7.5f,30,2.8f,2.7f);Anchor("Surface_Radio","Surface",-34,30,"B接电台入口，D接老周");Anchor("Surface_Toolshop","Surface",-21,30,"B接装备/升级，D接小林");Anchor("Surface_CaveMouth","Surface",-12,30,"先步行入洞，非工具棚内下井");
        }
        static void A()
        {
            var p=Region("A");Ground(p,"A_StartingPocket",2,18,0,7,.12f);Ground(p,"A_FirstLanding",21,44,0,7,.32f);Ground(p,"A_Catch",17,24,-4,2,.1f);Ladder(p,"A_Recovery",17.5f,-4,0);
            Ground(p,"A_OptionalShelf",30,39,6,2,.25f);Ladder(p,"A_Optional",36,0,6);Art("Crate",p,34,6.6f,1.2f,1.2f,25);
            Roof(p,"A_Roof",3,38,14,1.7f);Roof(p,"AB_Neck",38,47,8,1.2f);Lamp(p,7,4.5f);Lamp(p,32,9);Fungi(p,12,0,1.4f);
            Anchor("A_MothRecord","A",10,0,"S01：首跳前录音，本地蛾资源由D提供");Anchor("A_FirstJump","A",19.5f,0,"保留3单位首跳，不桥接");Anchor("A_CatchLedge","A",19.5f,-4,"直接位于首跳下方，B验证掉落恢复");
        }
        static void B()
        {
            var p=Region("B");Ground(p,"B_EntryStep",44,48,0,7);Ground(p,"B_LeftRise",48,55,2,8);Ladder(p,"B_Entry",48,0,2);
            Bridge(p,"B",55,64,2,.55f);Ground(p,"B_Recovery",54,65,-2,3,.22f);Ladder(p,"B_Recovery",64,-2,2);
            Ground(p,"B_MushroomBowl",64,79,2,8,.7f);Ground(p,"B_Terrace",79,88,4,7,.45f);Ground(p,"B_ExitTerrace",88,96,6,7,.35f);Ladder(p,"B_RiseOne",79,2,4);Ladder(p,"B_RiseTwo",88,4,6);Ladder(p,"B_ToC",96,6,8);
            Roof(p,"B_Vault",47,88,18,2);Roof(p,"BC_Neck",88,99,13,1.7f);
            foreach(float x in new[]{50f,52,65,68,70,74,77,83,91})Fungi(p,x,x<55?2:x<79?2:x<88?4:6,x==70?3.2f:1.6f);
            Lamp(p,51,7);Lamp(p,84,10);Device(p,"B_DripBucket","ControlBox",76,2,1.1f,1.4f);
        }
        static void C()
        {
            var p=Region("C");Ground(p,"C_LowerMine",96,140,-4,8,.45f);Ground(p,"C_MidRecovery",104,116,1,2,.2f);
            Wood(p,"C_LeftHighwalk",96,108,8);Bridge(p,"C",108,124,8,.75f);Wood(p,"C_RightHighwalk",124,140,8);Ladder(p,"C_RightDescent",138,-4,8);
            Roof(p,"C_HighFissure",99,135,22,3);Roof(p,"CD_Neck",135,144,13,1.6f);Lamp(p,102,12);Lamp(p,135,12);Fungi(p,109,-4,1.5f);Fungi(p,129,-4,1.2f);
            Anchor("C_MouseLureLeft","C",104,-4,"E/回放诱鼠调查左侧；不另造怪物逻辑");Anchor("C_MouseSpawn","C",113,-4,"D圆耳短尾矿鼠，B接AI");Anchor("C_Observation","C",102,8,"安全高廊观察位");
        }
        static void D()
        {
            var p=Region("D");Ground(p,"D_LowerPassage",140,190,-4,8,.3f);Wood(p,"D_OverBeastTimberBridge",152,186,4);Ladder(p,"D_BeastAccess",153,-4,4);Ladder(p,"D_Exit",186,-4,4);
            Roof(p,"D_LowRoof",144,191,12,1.7f);Lamp(p,146,3);Lamp(p,177,8);Fungi(p,150,-4);Fungi(p,184,-4);
            Device(p,"D_S02Record","ControlBox",146,-4,1.5f,1.5f);Device(p,"D_SpeakerRight","Speaker",181,-4,2,2.7f);
            Anchor("D_StonebackSpawn","D",164,-4,"小石背兽位置，非Boss；正式美术/AI待D/B");
        }
        static void Descent()
        {
            var p=Node("D_E_NaturalDescent",env).transform;
            Ground(p,"DE_UpperShelf",190,198,-4,3);Ground(p,"DE_MidOne",189,210,-14,3);Ground(p,"DE_MidTwo",190,210,-25,3);Ground(p,"DE_LowerShelf",190,210,-36,5);
            Ladder(p,"DE_One",194,-14,-4);Ladder(p,"DE_Two",206,-25,-14);Ladder(p,"DE_Three",194,-36,-25);
            for(int i=0;i<3;i++){Lamp(p,199,-8-i*11);Art("Stalactite",p,211,-8-i*10,3,5,25);}
            Anchor("D_E_Descent","D-E",198,-19,"自然洞道折向下行；不是第二口升降井");
        }
        static void E()
        {
            var p=Region("E");Ground(p,"E_CleanControls",177,210,-36,6,.18f);Ground(p,"E_DustPassage",164,177,-36,6,.2f);Ground(p,"E_SafeCP",153,164,-36,6,.12f);Roof(p,"E_PipeRoof",157,207,-28,1.1f);
            for(float x=164;x<207;x+=2){Pixel("PipeMain",p,x,-32.4f,2,.3f,new Color(.24f,.26f,.22f),16);Pixel("PipeJoint",p,x,-32.4f,.15f,.52f,Gold,17);}
            Device(p,"E_Valve","Valve",198,-36,2.8f,2.7f);Device(p,"E_S03Emitter","ControlBox",191,-36,2,2);Device(p,"E_S03Receiver","ControlBox",185,-36,2,2);Device(p,"E_FanSwitch","ControlBox",180,-36,1.7f,1.7f);Device(p,"E_Fan","Fan",172,-31,3.2f,3.2f);
            Device(p,"E_PurificationGate","Gate",164,-36,2,5,new[]{"待净化_门闭","净化中_门闭","已净化_门开"});
            for(int j=0;j<28;j++)Pixel("DustPreview",p,166+(float)rng.NextDouble()*10,-35+(float)rng.NextDouble()*2.2f,.12f,.12f,new Color(.8f,.52f,.23f,.25f),25);
            Device(p,"CP01","ControlBox",157,-36,1.3f,1.5f);Lamp(p,202,-29);Lamp(p,156,-30);
        }
        static void F()
        {
            var p=Region("F");Ground(p,"F_CounterGround",94,154,-44,7,.06f);Wood(p,"F_RightEntryLedge",143,154,-36);Wood(p,"F_LeftAlarmLedge",94,102,-35);Ladder(p,"F_AlarmEscape",99,-44,-35);Ladder(p,"F_Entry",148,-44,-36);
            Roof(p,"F_HighHall",98,152,-20,2.5f);Device(p,"F_RightSpeaker","Speaker",149,-44,2.5f,3);Device(p,"F_EntryControl","ControlBox",150,-36,1.4f,1.7f);Device(p,"F_LeftAlarm","ControlBox",96,-35,1.5f,2);
            var block=Node("Device_F_Blocker",p,104,-44);Pixel("FixedImpactBarrier",block.transform,0,.5f,2.3f,1,Gold,24);for(int i=0;i<5;i++)Line("HazardStripe",block.transform,new Vector2(-1+i*.46f,.1f),new Vector2(-.6f+i*.46f,.9f),.16f,Dark,25);block.AddComponent<BoxCollider2D>().offset=new Vector2(0,.5f);block.GetComponent<BoxCollider2D>().size=new Vector2(2.3f,1);
            Anchor("F_Blocker","F",104,-44,"固定低挡块；B决定冲撞响应/敲地波通过，不是动物");
            Device(p,"F_LeftGate","Gate",94,-44,2,5,new[]{"遭遇前关闭","三次反击完成_可操作","已打开"});Device(p,"F_ExitLever","ControlBox",96,-44,1,1.5f);
            Anchor("F_GuardianSpawn","F",125,-44,"Boss正式外形待D，非致命三次反击后右退；当前无动物假模型");Anchor("F_GroundCounter","F",101,-44,"回到下层地面向右敲地；不是高台跨层攻击");Anchor("F_RetreatRest","F",140,-44,"驱离后右侧休息位置");
            Lamp(p,109,-27);Lamp(p,138,-27);Fungi(p,112,-44);Fungi(p,134,-44);
        }
        static void G()
        {
            var p=Region("G");Ground(p,"G_QuietStation",2,94,-44,8,.16f);Roof(p,"G_StationRoof",3,88,-31,1.7f);Roof(p,"GF_Neck",86,96,-35,1.3f);
            Wood(p,"G_BackStationTimber",17,79,-42,false); // Back-only dressing, collider removed below.
            var back=p.Find("G_BackStationTimber");if(back){var c=back.GetComponent<Collider2D>();if(c)UnityEngine.Object.DestroyImmediate(c);foreach(var sr in back.GetComponentsInChildren<SpriteRenderer>())sr.sortingOrder=-10;}
            Device(p,"G_S04Record","Speaker",80,-44,2.3f,2.6f);Device(p,"G_RouteMap","Sign",52,-44,4.2f,3.8f);Device(p,"G_ReturnPower","ControlBox",20,-44,2.5f,2.8f);
            for(float x=12;x<84;x+=11){Art("Timber",p,x,-40.7f,3.3f,6.8f,15);Lamp(p,x+1,-37.5f);Art("Crate",p,x-2,-43.4f,1.2f,1.2f,22);}Fungi(p,35,-44,1.4f);Fungi(p,65,-44,1.3f);
        }
        static void Shaft()
        {
            var p=Node("MainShaft",env).transform;
            Pixel("ShaftRear",p,0,-7,4,82,new Color(.018f,.026f,.04f),-15);
            for(float y=-47;y<=41;y+=2.5f){Pixel("GuideLeft",p,-1.8f,y,.25f,2.5f,Gold*.65f,18);Pixel("GuideRight",p,1.8f,y,.25f,2.5f,Gold*.65f,18);Line("TimberCrossbrace",p,new Vector2(-1.8f,y),new Vector2(1.8f,y+2.3f),.14f,Gold*.4f,-12);}
            foreach(var pair in new[]{new[]{"Surface","30"},new[]{"A","0"},new[]{"G","-44"}})
            {
                float y=float.Parse(pair[1]);Anchor("Shaft_"+pair[0],pair[0],0,y,"同一主井停靠；B接升降与门联锁，G接电后可用");
                Pixel("LandingBeam",p,0,y-.25f,4.5f,.35f,Gold,21);
                Device(p,"ShaftDoor_"+pair[0],"Gate",0,y,3.5f,4.6f,new[]{"门闭","到站开门","运行锁门"});Lamp(p,-2.5f,y+4.5f);
            }
            var cage=Node("LiftCage_VisualOnly",p,0,0);Pixel("CageFloor",cage.transform,0,0,3.1f,.25f,Gold,24);Pixel("CageRoof",cage.transform,0,4,3.1f,.25f,Gold,24);foreach(float x in new[]{-1.4f,1.4f})Pixel("CagePost",cage.transform,x,2,.18f,4,Gold,25);
        }
        static void WritePoints()
        {
            Directory.CreateDirectory("Docs/EchoCaves");
            var lines=new List<string>{"ID,区域,X,Y,接入说明"};foreach(var a in Anchors)lines.Add($"{a.Id},{a.Region},{a.transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},{a.transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"{a.Handoff.Replace("\"","\"\"")}\"");
            File.WriteAllLines("Docs/EchoCaves/实际场景锚点.csv",lines,new System.Text.UTF8Encoding(true));
        }
        public static string Capture(string folder="Evidence/EchoCaves")
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Open Echo chapter first");
            Directory.CreateDirectory(folder);var c=Camera.main;var rev=c.GetComponent<EchoSceneReview>();var old=c.transform.position;int oldBookmark=rev.CurrentBookmark;
            var layers=UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None);var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();bool active=canvas&&canvas.gameObject.activeSelf;if(canvas)canvas.gameObject.SetActive(false);
            for(int i=0;i<rev.Bookmarks.Length;i++)
            {
                rev.JumpTo(i);foreach(var l in layers)l.Apply(c.transform.position);
                var rt=new RenderTexture(1536,864,24);rt.filterMode=FilterMode.Point;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1536,864,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1536,864),0,0);t.Apply();File.WriteAllBytes(folder+"/"+i.ToString("00")+"-scene.png",t.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t);
            }
            rev.CurrentBookmark=oldBookmark;c.transform.position=old;foreach(var l in layers)l.Apply(old);if(canvas)canvas.gameObject.SetActive(active);
            return "Captured "+rev.Bookmarks.Length+" real Unity camera views";
        }
    }
}
