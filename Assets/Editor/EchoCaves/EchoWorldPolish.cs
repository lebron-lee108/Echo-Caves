using System;
using System.Linq;
using EchoCaves.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static EchoCaves.Editor.EchoEnvironmentArt;

namespace EchoCaves.Editor
{
    public static class EchoWorldPolish
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            if(Sprites.Count==0)Prepare();
            var surface=GameObject.Find("Region_Surface").transform;
            var outdoors=surface.Find("OutsideBackdrop").GetComponent<SpriteRenderer>();outdoors.transform.localPosition=new Vector3(-39,38);outdoors.transform.localScale=new Vector3(54/outdoors.sprite.bounds.size.x,30/outdoors.sprite.bounds.size.y,1);
            if(!surface.Find("CaveEntranceDepth"))
            {
                var parent=Node("CaveEntranceDepth",surface).transform;Art("FarCavern",parent,-1,36,24,22,-88,new Color(.45f,.53f,.68f));Art("Timber",parent,-11,34,3.3f,8,27);Art("Timber",parent,-1,34,3.3f,8,27);
                // Right wall closes only first-layer entrance hub, never A-D exploration.
                Rock("Surface_EndWall",parent,new[]{new Vector2(5,29),new Vector2(6,32),new Vector2(5.5f,36),new Vector2(6,41),new Vector2(16,41)},14,25,true);
            }
            var parentRoot=GameObject.Find("Echo Caves — Chapter 01 · C handoff").transform;
            var oldDepth=parentRoot.Find("DescentDepthLayer");
            if(!oldDepth)
            {
                var g=Node("DescentDepthLayer",parentRoot);var p=g.AddComponent<EchoParallaxLayer>();p.TargetCamera=Camera.main;p.RelativeSpeed=.18f;p.ReferenceCamera=Camera.main.transform.position;p.ReferencePosition=g.transform.position;
                Art("FarCavern",g.transform,198,-16,82,65,-101,new Color(.38f,.46f,.58f));
            }
            var matPath=Root+"Materials/DepthFade.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!material){material=new Material(Shader.Find("EchoCaves/DepthEdgeFade"));AssetDatabase.CreateAsset(material,matPath);}
            foreach(var l in UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None))foreach(var s in l.GetComponentsInChildren<SpriteRenderer>())s.sharedMaterial=material;
            // Overlap the two depth rows through the lift journey; a fixed y=-18..-13 black band is not a cave.
            foreach(var l in UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None).Where(l=>l.name=="FarLayer"))
            foreach(var s in l.GetComponentsInChildren<SpriteRenderer>())
            {var scale=s.transform.localScale;scale.y=50/s.sprite.bounds.size.y;s.transform.localScale=scale;}
            var ceilings=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Stalactite").OrderBy(t=>t.position.x).ToArray();
            int k=0;foreach(var t in ceilings)
            {
                var s=t.GetComponent<SpriteRenderer>();if(!s)continue;float h=2.2f+(k%5)*.47f,w=2.0f+(k%3)*.41f;t.localScale=new Vector3(w/s.sprite.bounds.size.x,h/s.sprite.bounds.size.y,1);s.color=new Color(.55f+(k%3)*.1f,.64f,.75f);k++;
            }
            foreach(var v in UnityEngine.Object.FindObjectsByType<EchoVisualState>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!(v.name.Contains("Gate")||v.name.Contains("ShaftDoor")))continue;
                for(int i=0;i<v.StateNames.Length;i++)if(v.StateNames[i].Contains("已打开")||v.StateNames[i].Contains("门开")||v.StateNames[i].Contains("开门"))
                {
                    var art=v.StateArt[i].GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(s=>s.name=="Gate");if(!art)continue;
                    float w=art.sprite.bounds.size.x*art.transform.localScale.x,h=art.sprite.bounds.size.y*art.transform.localScale.y;art.enabled=false;
                    if(!v.StateArt[i].transform.Find("OpenFrame"))
                    {var frame=Node("OpenFrame",v.StateArt[i].transform).transform;Pixel("OpenFrame",frame,-w*.48f,0,.16f,h,new Color(.47f,.32f,.17f),28);Pixel("FrameRight",frame,w*.48f,0,.16f,h,new Color(.47f,.32f,.17f),28);Pixel("FrameTop",frame,0,h*.47f,w,.22f,new Color(.7f,.48f,.2f),28);}
                }
            }
            // Background station dressing stays behind the playable floor, not an extra walkable deck.
            var station=GameObject.Find("Region_G").transform.Find("G_BackStationTimber");if(station)station.gameObject.SetActive(false);
            var dark=new Color(.12f,.10f,.08f);foreach(var pos in new[]{new Vector2(31,-38),new Vector2(63,-38)})
            {
                string name="StationCables_"+pos.x;if(GameObject.Find(name))continue;var root=Node(name,GameObject.Find("Region_G").transform).transform;
                for(int i=0;i<10;i++){var a=new Vector2(pos.x-7+i*1.4f,pos.y-1.2f*Mathf.Sin(i/10f*Mathf.PI));var b=new Vector2(pos.x-7+(i+1)*1.4f,pos.y-1.2f*Mathf.Sin((i+1)/10f*Mathf.PI));Line("Cable",root,a,b,.08f,dark,18);}
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return EchoSceneChecks.ValidateWorldDesign();
        }
    }
}
