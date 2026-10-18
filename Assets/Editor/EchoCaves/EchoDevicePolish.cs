using System;
using EchoCaves.Scene;
using UnityEditor.SceneManagement;
using UnityEngine;
using static EchoCaves.Editor.EchoEnvironmentArt;

namespace EchoCaves.Editor
{
    public static class EchoDevicePolish
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");if(Sprites.Count==0)Prepare();
            var descent=GameObject.Find("D_E_NaturalDescent").transform;
            if(!descent.Find("DE_EastWall"))Rock("DE_EastWall",descent,new[]{new Vector2(211,-37),new Vector2(215,-37)},35,21,true);
            if(!descent.Find("DE_WestWall"))Rock("DE_WestWall",descent,new[]{new Vector2(185,-29),new Vector2(189,-29)},24,21,true);
            foreach(var id in new[]{"Surface_Map","G_RouteMap"})
            {
                var v=GameObject.Find("Device_"+id).GetComponent<EchoVisualState>();
                foreach(var state in v.StateArt)
                {
                    foreach(var sr in state.GetComponentsInChildren<SpriteRenderer>(true))if(sr.name=="Sign")sr.enabled=false;
                    if(state.transform.Find("RouteBoard"))continue;
                    var p=Node("RouteBoard",state.transform).transform;var wood=new Color(.27f,.17f,.09f);var paper=new Color(.48f,.43f,.29f);var ink=new Color(.12f,.18f,.20f);
                    Pixel("WoodFrame",p,0,0,4.5f,3.2f,wood,28);Pixel("AgedMap",p,0,0,4.1f,2.8f,paper,29);
                    foreach(float x in new[]{-2f,2f})Pixel("SupportPost",p,x,-2.0f,.18f,1.6f,wood,27);
                    var pts=new[]{new Vector2(-1.5f,.9f),new Vector2(-.7f,.9f),new Vector2(-.7f,.1f),new Vector2(1.5f,.1f),new Vector2(1.5f,-.8f),new Vector2(-.7f,-.8f)};
                    for(int i=1;i<pts.Length;i++)Line("Route",p,pts[i-1],pts[i],.07f,ink,30);
                    foreach(var pt in pts)Pixel("Stop",p,pt.x,pt.y,.18f,.18f,new Color(.73f,.56f,.28f),31);
                    Line("ReturnShaft",p,new Vector2(-.7f,-.8f),new Vector2(-.7f,.9f),.05f,new Color(.2f,.65f,.68f),31);
                    for(int i=0;i<7;i++)Pixel("PaperWear",p,-1.8f+i*.55f,1.22f,.21f,.0625f,wood,30);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Cave descent framed by geology; cave entrance and G now carry route boards, not generic direction arrows";
        }
    }
}
