using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoCaves.Editor
{
    public static class EchoRoutePolish
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            ShiftLadder("A_Recovery",18.7f);ShiftLadder("B_Recovery",63.3f);ShiftLadder("B_Entry",47.3f);ShiftLadder("B_RiseOne",78.3f);ShiftLadder("B_RiseTwo",87.3f);ShiftLadder("B_ToC",95.3f);
            foreach(var name in new[]{"C_LeftHighwalk","C_RightHighwalk","D_OverBeastTimberBridge","F_RightEntryLedge","F_LeftAlarmLedge","A_OptionalShelf"})
            {
                var g=GameObject.Find(name);var p=g.GetComponent<PlatformEffector2D>();if(!p)p=g.AddComponent<PlatformEffector2D>();p.useOneWay=true;p.surfaceArc=170;g.GetComponent<Collider2D>().usedByEffector=true;
            }
            GameObject.Find("Anchor_D_StonebackSpawn").transform.position=new Vector3(150,-4);
            GameObject.Find("Anchor_D_RecordFallback").transform.position=new Vector3(182,-4);
            GameObject.Find("Anchor_D_SpeakerRight").transform.position=new Vector3(177,-4);
            var speaker=GameObject.Find("Device_D_SpeakerRight");var speakerPos=speaker.transform.position;speakerPos.x=177;speaker.transform.position=speakerPos;
            // Alternating shelf edges leave real ladder wells, rather than expecting a controller to phase through rock.
            ResizeShelf("DE_MidOne",198,210);ResizeShelf("DE_MidTwo",189,199);ResizeShelf("E_PipeRoof",157,189);
            RepairDressing("DE_MidOne",189,210,198,210,-14,false);
            RepairDressing("DE_MidTwo",190,210,189,199,-25,false);
            RepairDressing("E_PipeRoof",157,207,157,189,-28,true);
            ShiftLadder("DE_One",198.7f);ShiftLadder("DE_Two",197.3f);ShiftLadder("DE_Three",199.7f);
            var report=EchoSceneChecks.ValidateRouteGeometry();
            if(!report.Contains(": PASS;"))throw new InvalidOperationException(report+"; scene not saved by route polish");
            EchoHandoffAssembly.CompleteAnchors();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return report;
        }
        static void RepairDressing(string name,float oldMin,float oldMax,float min,float max,float y,bool roof)
        {
            var g=GameObject.Find(name);if(g.transform.Find("DressingAligned_V2"))return;
            foreach(Transform t in g.transform.parent)
            {
                bool match=roof?(t.name=="Stalactite"||t.name=="Vine"):(t.name=="MossLip"||t.name=="Fern"||t.name=="Rubble");
                var pos=t.localPosition;if(!match||pos.x<oldMin||pos.x>oldMax||Mathf.Abs(pos.y-y)>(roof?4:.8f))continue;
                pos.x=Mathf.LerpUnclamped(min,max,(pos.x-oldMin)/(oldMax-oldMin));t.localPosition=pos;
                if(t.name=="MossLip"){var s=t.localScale;s.x*=(max-min)/(oldMax-oldMin);t.localScale=s;}
            }
            EchoEnvironmentArt.Node("DressingAligned_V2",g.transform);
        }
        static void ShiftLadder(string id,float x)
        {
            var g=GameObject.Find("Ladder_"+id);var box=g.GetComponent<BoxCollider2D>();float delta=x-box.offset.x;
            foreach(Transform t in g.transform){var p=t.localPosition;p.x+=delta;t.localPosition=p;}
            box.offset=new Vector2(x,box.offset.y);
        }
        static void ResizeShelf(string name,float min,float max)
        {
            var g=GameObject.Find(name);var c=g.GetComponent<PolygonCollider2D>();float oldMin=c.points.Min(p=>p.x),oldMax=c.points.Max(p=>p.x);
            if(Mathf.Abs(oldMin-min)<.01f&&Mathf.Abs(oldMax-max)<.01f)return;
            float X(float x)=>Mathf.LerpUnclamped(min,max,Mathf.InverseLerp(oldMin,oldMax,x));
            var mesh=UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);var vertices=mesh.vertices;var uv=mesh.uv;
            for(int i=0;i<vertices.Length;i++){vertices[i].x=X(vertices[i].x);uv[i].x=vertices[i].x/8;}
            mesh.vertices=vertices;mesh.uv=uv;mesh.RecalculateBounds();mesh.name=name+"_RouteV2";
            string path="Assets/_EchoCaves/Meshes/"+mesh.name+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(path))throw new Exception("Existing revised mesh; preserve manual work");
            AssetDatabase.CreateAsset(mesh,path);g.GetComponent<MeshFilter>().sharedMesh=mesh;c.points=c.points.Select(p=>new Vector2(X(p.x),p.y)).ToArray();
            var rim=g.transform.Find("GeologyRim");if(rim)UnityEngine.Object.DestroyImmediate(rim.gameObject); // Only our generated overlay, recreated by geology polish.
        }
    }
}
