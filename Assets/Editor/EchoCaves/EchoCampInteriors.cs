using System;
using System.Linq;
using EchoCaves.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static EchoCaves.Editor.EchoEnvironmentArt;

namespace EchoCaves.Editor
{
    public static class EchoCampInteriors
    {
        public static string Build()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            if(GameObject.Find("CampInteriors"))throw new Exception("Preserve existing interior authoring");
            Prepare();var root=Node("CampInteriors",GameObject.Find("Echo Caves — Chapter 01 · C handoff").transform).transform;
            var radio=Node("RadioInterior",root).transform;var shop=Node("WorkshopInterior",root).transform;
            Room(radio,-110);Room(shop,-170);
            Art("RadioRoom",radio,-110,35.12f,48,16,-8);
            Art("RadioRoom",shop,-170,35.12f,48,16,-8,new Color(.83f,.87f,.93f));
            // All operating furniture is a separate replaceable sprite, never baked with a character.
            Art("RadioPlayer",radio,-108,32.2f,6.1f,4.4f,22);
            Art("Transmitter",radio,-94,34.5f,4.5f,9,20);
            Art("TapeShelf",radio,-124,34.2f,5.5f,8.4f,19);
            Art("Workbench",radio,-115,32.1f,8,4.2f,16);
            Art("Workbench",shop,-170,32.4f,10,4.8f,21);
            Art("TapeShelf",shop,-186,34.2f,5.5f,8.4f,19);
            Art("ControlBox",shop,-160,31.6f,2.8f,3.2f,22);
            Art("Crate",shop,-153,31,2.2f,2,22);
            void Anchor(string id,Transform p,float x,string note)
            {var a=Node("Anchor_"+id,p,x,30).AddComponent<EchoAnchor>();a.Id=id;a.Region="SurfaceInterior";a.Handoff=note;}
            Anchor("Surface_RadioPlayer",radio,-108,"单播放器，B接60秒播出与UI，不存在双台切换");
            Anchor("Surface_RadioExit",radio,-130,"经营室内返回洞外电台入口；B接场景过渡，不连接探索洞室");
            Anchor("Surface_Zhou",radio,-128,"老周留在洞外经营区域，D供角色；不下井");
            Anchor("Surface_UpgradeBench",shop,-170,"可选18元升级台；B接购买，D供小林角色");
            Anchor("Surface_WorkshopExit",shop,-190,"返回洞外工具棚入口；B接过渡");
            var review=Camera.main.GetComponent<EchoSceneReview>();review.Bookmarks=review.Bookmarks.Concat(new[]{new Vector2(-110,36),new Vector2(-170,36)}).ToArray();review.BookmarkNames=review.BookmarkNames.Concat(new[]{"洞外电台室内","洞外升级工具棚室内"}).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return EchoHandoffAssembly.ExportAnchors();
        }
        static void Room(Transform parent,float x)
        {
            Pixel("RoomDepth",parent,x,36,49,28,new Color(.025f,.035f,.06f),-9);
            var floor=Node("WalkableRoomFloor",parent,x,29.8f);var c=floor.AddComponent<BoxCollider2D>();c.size=new Vector2(47,.4f);
            foreach(float edge in new[]{x-24,x+24}){var wall=Node("RoomSideBoundary",parent,edge,35).AddComponent<BoxCollider2D>();wall.size=new Vector2(.4f,12);}
        }
    }
}
