using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoCaves.Editor
{
    public static class EchoSceneChecks
    {
        public const string ScenePath="Assets/_EchoCaves/Scenes/Chapter01_Continuous.unity";
        public static string Validate()
        {
            var issues=new List<string>(); int checks=0;
            Action<bool,string> test=(ok,label)=>{checks++;if(!ok)issues.Add(label);};
            test(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null,"chapter scene missing");
            var scene=SceneManager.GetActiveScene();
            test(scene.path==ScenePath,"wrong active scene");
            if(scene.path==ScenePath)
            {
                var roots=scene.GetRootGameObjects(); var all=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach(var region in new[]{"Surface","A","B","C","D","E","F","G"})test(all.Any(x=>x.name=="Region_"+region),"missing region "+region);
                test(all.Count(x=>x.name=="MainShaft")==1,"exactly one shaft");
                test(all.Count(x=>x.name.StartsWith("RopeBridge_"))==2,"exactly two rope bridges");
                test(!all.Any(x=>x.name.IndexOf("Minecart",StringComparison.OrdinalIgnoreCase)>=0),"no minecart");
                foreach(var id in new[]{"A_MothRecord","A_FirstJump","A_CatchLedge","B_RopeBridge","C_RopeBridge","C_MouseLureLeft","D_S02Record","D_SpeakerRight","E_Valve","E_S03Emitter","E_S03Receiver","E_FanSwitch","E_PurificationGate","CP01","F_RightSpeaker","F_LeftAlarm","F_Blocker","F_LeftGate","G_S04Record","G_RouteMap","G_ReturnPower","Shaft_Surface","Shaft_A","Shaft_G"})test(all.Any(x=>x.name=="Anchor_"+id),"anchor "+id);
                var cams=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).ToArray();test(cams.Length==1,"one camera");
                if(cams.Length==1){test(cams[0].orthographic,"orthographic camera");test(!cams[0].allowMSAA&&!cams[0].allowHDR,"pixel camera AA/HDR off");}
                test(all.Count(x=>x.GetComponent<Collider2D>()!=null)>30,"editable colliders");
                test(all.Count(x=>x.GetComponent<SpriteRenderer>()!=null)>100,"modular renderers not a flat full-scene poster");
                test(all.Any(x=>x.name=="FarLayer")&&all.Any(x=>x.name=="MidLayer")&&all.Any(x=>x.name=="NearLayer"),"parallax layer roots");
                test(all.All(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)==0),"no missing scripts");
                var anchorNames=new[]{"Anchor_Shaft_Surface","Anchor_Shaft_A","Anchor_Shaft_G"};
                var shaftAnchors=all.Where(x=>anchorNames.Contains(x.name)).ToArray();
                test(shaftAnchors.Length==3&&shaftAnchors.All(x=>Mathf.Abs(x.position.x)<.01f),"surface A G same shaft x=0");
                var a=all.FirstOrDefault(x=>x.name=="Anchor_A_FirstJump");var b=all.FirstOrDefault(x=>x.name=="Anchor_A_CatchLedge");test(a&&b&&b.position.y<a.position.y&&Mathf.Abs(a.position.x-b.position.x)<2,"first gap catch directly beneath");
            }
            var msg=$"Echo native scene checks: {checks-issues.Count}/{checks}; "+(issues.Count==0?"PASS":"FAIL: "+string.Join(" | ",issues));
            Debug.Log(msg);return msg;
        }
        public static string ValidatePresentation()
        {
            var issues=new List<string>();int n=0;Action<bool,string> test=(ok,s)=>{n++;if(!ok)issues.Add(s);};
            foreach(var key in new[]{"RadioHut","ToolShed","Lantern","Mushroom"})
            {
                foreach(var asset in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/_EchoCaves/Art"}))
                foreach(var s in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(asset)).OfType<Sprite>().Where(s=>s.name==key))
                {
                    var t=new Texture2D(2,2);t.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(s)));var pixels=t.GetPixels32();var r=s.rect;int minY=(int)r.yMax,maxY=(int)r.y;
                    for(int y=(int)r.y;y<(int)r.yMax;y++)for(int x=(int)r.x;x<(int)r.xMax;x++)if(pixels[y*t.width+x].a>100){minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                    test(minY-r.y<5&&r.yMax-maxY<5,key+" has excessive transparent vertical padding");UnityEngine.Object.DestroyImmediate(t);
                }
            }
            foreach(var l in UnityEngine.Object.FindObjectsByType<EchoCaves.Scene.EchoParallaxLayer>(FindObjectsSortMode.None))
            {var original=l.transform.position;l.Apply(l.ReferenceCamera+new Vector3(10,0));test(Mathf.Abs(l.transform.position.x-l.ReferencePosition.x-10*(1-l.RelativeSpeed))<.001f,"parallax displacement "+l.name);l.transform.position=original;}
            var root=GameObject.Find("EchoCavesUI");test(root!=null,"UI root present");
            return $"Presentation checks {n-issues.Count}/{n}: "+(issues.Count==0?"PASS":string.Join(" | ",issues));
        }
        public static string ValidateWorldDesign()
        {
            var issues=new List<string>();int n=0;Action<bool,string> test=(ok,s)=>{n++;if(!ok)issues.Add(s);};
            Vector3 P(string id)=>GameObject.Find("Anchor_"+id).transform.position;
            test(P("Surface_Map").x>P("Surface_CaveMouth").x&&P("Surface_Map").x<0,"walk inside and see map before shaft");
            var outdoors=GameObject.Find("OutsideBackdrop").GetComponent<SpriteRenderer>();test(outdoors.bounds.max.x<=-11.9f,"outdoor sky must end at cave mouth");
            bool covered=false;foreach(var l in UnityEngine.Object.FindObjectsByType<EchoCaves.Scene.EchoParallaxLayer>(FindObjectsSortMode.None))if(l.RelativeSpeed<.3f)foreach(var s in l.GetComponentsInChildren<SpriteRenderer>())if(s.bounds.Contains(new Vector3(198,-15,0)))covered=true;
            test(covered,"continuous far backdrop behind D-E descent, no horizontal black band");
            bool shaftCovered=false;foreach(var l in UnityEngine.Object.FindObjectsByType<EchoCaves.Scene.EchoParallaxLayer>(FindObjectsSortMode.None))if(l.name=="FarLayer")foreach(var s in l.GetComponentsInChildren<SpriteRenderer>())if(s.bounds.Contains(new Vector3(0,-15,0)))shaftCovered=true;
            test(shaftCovered,"far backdrop remains continuous during original shaft travel");
            float gate=P("E_PurificationGate").x;foreach(var id in new[]{"E_Valve","E_S03Emitter","E_S03Receiver","E_FanSwitch"})test(P(id).x>gate,"E clean control right of gate "+id);
            test(P("CP01").x<gate,"CP safe LEFT of purification gate");
            test(P("F_GroundCounter").y==P("F_GuardianSpawn").y,"groundwave counter on same lower ground");
            test(P("G_S04Record").x>P("G_RouteMap").x&&P("G_RouteMap").x>P("G_ReturnPower").x&&P("G_ReturnPower").x>0,"G right-to-left evidence order before original shaft");
            var gates=UnityEngine.Object.FindObjectsByType<EchoCaves.Scene.EchoVisualState>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var v in gates.Where(v=>v.name.Contains("Gate")||v.name.Contains("ShaftDoor")))for(int i=0;i<v.StateNames.Length;i++)if(v.StateNames[i].Contains("已打开")||v.StateNames[i].Contains("门开")||v.StateNames[i].Contains("开门"))
                test(v.StateArt[i].GetComponentsInChildren<SpriteRenderer>(true).Any(s=>s.name=="OpenFrame"),v.name+" open state needs genuinely open artwork");
            return $"World design checks {n-issues.Count}/{n}: "+(issues.Count==0?"PASS":string.Join(" | ",issues));
        }
        public static string ValidateDepthTint()
        {
            var scene=EditorSceneManager.NewPreviewScene();var rt=new RenderTexture(64,64,0);var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
            var old=RenderTexture.active;Material mat=null;
            try
            {
                var go=new GameObject("TintProbe");SceneManager.MoveGameObjectToScene(go,scene);var sr=go.AddComponent<SpriteRenderer>();
                sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_EchoCaves/Art/FarCavern.png");
                mat=new Material(Shader.Find("EchoCaves/DepthEdgeFade"));sr.sharedMaterial=mat;
                var cg=new GameObject("ProbeCamera");SceneManager.MoveGameObjectToScene(cg,scene);var c=cg.AddComponent<Camera>();c.scene=scene;c.orthographic=true;c.orthographicSize=1;c.transform.position=new Vector3(0,0,-10);c.targetTexture=rt;c.allowHDR=false;c.allowMSAA=false;
                Color Sample(Color tint){sr.color=tint;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,64,64),0,0);tex.Apply();return tex.GetPixel(32,32);}
                var white=Sample(Color.white);var dark=Sample(new Color(.2f,.3f,.4f));
                bool pass=white.grayscale>.01f&&dark.grayscale<white.grayscale*.8f&&dark.b<white.b*.9f;
                return "Depth renderer tint GPU probe: "+(pass?"PASS":"FAIL")+" white="+white+" tinted="+dark;
            }
            finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static string ValidateRouteGeometry()
        {
            Physics2D.SyncTransforms();var issues=new List<string>();int n=0;
            void Test(bool ok,string label){n++;if(!ok)issues.Add(label);}
            foreach(string id in new[]{"A_Recovery","B_Recovery","B_Entry","B_RiseOne","B_RiseTwo","B_ToC","DE_One","DE_Two","DE_Three"})
            {
                var t=GameObject.Find("Ladder_"+id).transform;var c=t.GetComponent<BoxCollider2D>();
                var p=t.TransformPoint(c.offset-Vector2.up*(c.size.y-.7f)/2+Vector2.up*.8f);
                var hit=Physics2D.OverlapBoxAll(p,new Vector2(.6f,.5f),0).Where(h=>!h.isTrigger).ToArray();
                Test(hit.Length==0,id+" ladder body embedded in solid terrain");
            }
            Vector3 P(string id)=>GameObject.Find("Anchor_"+id).transform.position;
            Test(Vector2.Distance(P("D_S02Record"),P("D_StonebackSpawn"))<=6,"D initial recording beyond 6-unit radius");
            var ladder=GameObject.Find("Ladder_D_BeastAccess").GetComponent<BoxCollider2D>();
            Test(ladder.offset.x>P("D_StonebackSpawn").x+1,"D beast must guard access to bridge ladder before lure");
            Test(Vector2.Distance(P("D_RecordFallback"),P("D_SpeakerRight"))<=6,"D fallback recording beyond lure position radius");
            Test(Vector2.Distance(P("D_StonebackSpawn"),P("D_SpeakerRight"))<=28,"D speaker outside 28-unit hearing radius");
            foreach(var name in new[]{"C_LeftHighwalk","C_RightHighwalk","D_OverBeastTimberBridge","F_RightEntryLedge","F_LeftAlarmLedge"})
            {var go=GameObject.Find(name);Test(go.GetComponent<PlatformEffector2D>()&&go.GetComponent<Collider2D>().usedByEffector,name+" requires a pass-through ladder landing");}
            return $"Route geometry probes {n-issues.Count}/{n}: "+(issues.Count==0?"PASS":string.Join(" | ",issues))+"; structural probes, not a player-controller playtest";
        }
    }
}
