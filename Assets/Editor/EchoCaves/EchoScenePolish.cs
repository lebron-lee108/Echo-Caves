using System;
using System.Collections.Generic;
using System.Linq;
using EchoCaves.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static EchoCaves.Editor.EchoEnvironmentArt;

namespace EchoCaves.Editor
{
    public static class EchoScenePolish
    {
        public static string Apply()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            var renderers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var before=new List<(SpriteRenderer r,string key,float width,float height)>();
            foreach(var s in renderers)if(s.sprite&&AssetDatabase.GetAssetPath(s.sprite).StartsWith(Root))before.Add((s,s.sprite.name,s.sprite.bounds.size.x*s.transform.localScale.x,s.sprite.bounds.size.y*s.transform.localScale.y));
            Prepare();
            foreach(var p in before)if(p.r&&Sprites.TryGetValue(p.key,out var sprite)){p.r.sprite=sprite;p.r.transform.localScale=new Vector3(p.width/sprite.bounds.size.x,p.height/sprite.bounds.size.y,1);}
            foreach(var layer in UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None))
                foreach(var s in layer.GetComponentsInChildren<SpriteRenderer>())s.color=layer.RelativeSpeed<.3f?new Color(.38f,.46f,.58f):layer.RelativeSpeed<1?new Color(.46f,.53f,.65f):new Color(.10f,.14f,.20f,.48f);
            foreach(var s in renderers)if(s&&s.name=="LampGlow")s.enabled=false;
            SetBridge("B",55,64,2,3.2f,"BridgeShort");SetBridge("C",108,124,8,3.5f,"BridgeLong");
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(t.name.StartsWith("Ladder_"))StyleLadder(t);
                if(t.name=="C_LeftHighwalk")StyleCatwalk(t,96,108,8);
                if(t.name=="C_RightHighwalk")StyleCatwalk(t,124,140,8);
                if(t.name=="D_OverBeastTimberBridge")StyleCatwalk(t,152,186,4);
                if(t.name=="F_RightEntryLedge")StyleCatwalk(t,143,154,-36);
                if(t.name=="F_LeftAlarmLedge")StyleCatwalk(t,94,102,-35);
            }
            // Fill visible undercuts with textured geology, not new physics or game routes.
            foreach(var p in new[]{("A",8f,0f),("B",69f,2f),("C",128f,-4f),("D",174f,-4f),("E",195f,-36f),("G",59f,-44f)})
            {
                var region=GameObject.Find("Region_"+p.Item1).transform;if(region.Find("PolishedDressing"))continue;
                var dressing=Node("PolishedDressing",region).transform;
                for(int i=0;i<5;i++){float x=p.Item2-6+i*3.2f;Art("Rubble",dressing,x,p.Item3-1.7f,4,3.2f,12,new Color(.45f,.55f,.63f));if(i%2==0)Art("Vine",dressing,x+.8f,p.Item3-1.4f,.6f,2.7f,19);}
                if(p.Item1=="B")for(int i=0;i<7;i++)Art("Mushroom",dressing,65+i*2.1f,2.8f+(i%2)*.25f,1.4f+(i%3)*.35f,1.8f+(i%3)*.3f,22);
            }
            SetSpriteGround("RadioHut",30,10);SetSpriteGround("ToolShed",30,8);
            var c=Camera.main;if(c)c.transform.position=new Vector3(68,9,-20);
            foreach(var l in UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None))l.Apply(c.transform.position);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            return EchoSceneChecks.Validate()+"; staged polish: alpha-trimmed sprites, muted depth, detailed bridges/catwalks";
        }
        static void SetSpriteGround(string name,float y,float h)
        {var g=GameObject.Find(name);if(g){var pos=g.transform.position;pos.y=y+h/2;g.transform.position=pos;}}
        static void SetBridge(string id,float a,float b,float y,float h,string key)
        {
            var g=GameObject.Find("RopeBridge_"+id);foreach(var s in g.GetComponentsInChildren<SpriteRenderer>())s.enabled=false;
            var prior=g.transform.Find("FormalBridgeArt");if(prior){prior.gameObject.SetActive(true);foreach(var s in prior.GetComponentsInChildren<SpriteRenderer>())s.enabled=true;return;}
            var parent=Node("FormalBridgeArt",g.transform).transform;Art(key,parent,(a+b)/2,y+1.0f,b-a+.5f,h,25);
        }
        static void StyleCatwalk(Transform t,float a,float b,float y)
        {
            foreach(var s in t.GetComponentsInChildren<SpriteRenderer>())s.enabled=false;
            var old=t.Find("FormalCatwalkArt");if(old){foreach(var s in old.GetComponentsInChildren<SpriteRenderer>())s.enabled=true;return;}
            var root=Node("FormalCatwalkArt",t).transform;int n=Mathf.CeilToInt((b-a)/7f);float w=(b-a)/n;
            for(int i=0;i<n;i++)Art("Catwalk",root,a+w*(i+.5f),y-.4f,w+.12f,3.2f,22);
        }
        static void StyleLadder(Transform t)
        {
            var box=t.GetComponent<BoxCollider2D>();if(!box)return;
            foreach(var s in t.GetComponentsInChildren<SpriteRenderer>())s.enabled=false;
            var old=t.Find("FormalLadderArt");if(old){foreach(var s in old.GetComponentsInChildren<SpriteRenderer>())s.enabled=true;return;}
            var root=Node("FormalLadderArt",t).transform;float height=box.size.y-.7f;int n=Mathf.Max(1,Mathf.CeilToInt(height/5));float h=height/n;
            for(int i=0;i<n;i++)Art("LadderArt",root,box.offset.x,box.offset.y-height/2+h*(i+.5f),.95f,h+.1f,26);
        }
    }
}
