using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EchoCaves.Scene;
using static EchoCaves.Editor.EchoEnvironmentArt;

namespace EchoCaves.Editor
{
    /// <summary>Visual overlays only. Existing collision and designer route remain unchanged.</summary>
    public static class EchoGeologyPolish
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            Prepare();
            var rocks=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int ground=0,roof=0;
            foreach(var mr in rocks)
            {
                var pc=mr.GetComponent<PolygonCollider2D>();if(!pc||pc.points.Length<4||mr.transform.Find("GeologyRim"))continue;
                var pts=pc.points;int n=pts.Length-2;
                // Builder stores the playable/visible contour first and two closing corners last.
                bool ceiling=pts[n].y>pts.Take(n).Max(p=>p.y);
                string key=ceiling?"CeilingRim":"GroundRim";if(!Sprites.ContainsKey(key))continue;
                float min=pts.Take(n).Min(p=>p.x),max=pts.Take(n).Max(p=>p.x);
                if(max-min<1||mr.name.Contains("EndWall"))continue;
                var parent=Node("GeologyRim",mr.transform).transform;
                int count=Mathf.CeilToInt((max-min)/12f);float w=(max-min)/count;
                for(int j=0;j<count;j++)
                {
                    float x=min+(j+.5f)*w;float y=Contour(pts,n,x);float h=ceiling?Mathf.Min(3.8f,w*.32f):Mathf.Min(3.8f,w*.25f);
                    // Ground sprite top is aligned to collision; roof mass extends away from free space.
                    var s=Art(key,parent,x,ceiling?y+h*.22f:y-h*.5f+.08f,w+.08f,h,ceiling?24:15,new Color(.68f,.79f,.93f));
                    if(j%2==1)s.flipX=true;
                }
                if(ceiling)roof++;else ground++;
            }
            if(Sprites.TryGetValue("NearFraming",out var near))
            foreach(var layer in UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None).Where(p=>p.name=="NearLayer"))
            foreach(var sr in layer.GetComponentsInChildren<SpriteRenderer>())
            {
                var old=sr.bounds.size;sr.sprite=near;sr.transform.localScale=new Vector3(old.x/near.bounds.size.x,old.y/near.bounds.size.y,1);
                sr.color=new Color(.18f,.23f,.30f,.65f);sr.sortingOrder=40;
            }
            if(Sprites.ContainsKey("CeilingRim"))foreach(var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))if(sr.name=="Stalactite"&&sr.transform.parent.GetComponent<PolygonCollider2D>())sr.enabled=false;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Geology overlays added: ground="+ground+", roof="+roof+"; no collision geometry changed.";
        }
        static float Contour(Vector2[] p,int count,float x)
        {
            for(int i=1;i<count;i++)if(p[i].x>=x&&p[i].x>p[i-1].x)return Mathf.Lerp(p[i-1].y,p[i].y,Mathf.InverseLerp(p[i-1].x,p[i].x,x));
            return p[count-1].y;
        }
    }
}
