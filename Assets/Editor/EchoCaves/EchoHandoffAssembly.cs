using System;
using System.IO;
using System.Linq;
using System.Text;
using EchoCaves.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoCaves.Editor
{
    public static class EchoHandoffAssembly
    {
        [MenuItem("Echo Caves/Open Chapter 01")]
        public static void OpenChapter(){EditorSceneManager.OpenScene(EchoSceneChecks.ScenePath);}
        public static string CompleteAnchors()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            var parent=GameObject.Find("Echo Caves — Chapter 01 · C handoff").transform;
            void Add(string id,string r,float x,float y,string note,Vector2? range=null)
            {
                var old=parent.Find("Anchor_"+id);
                var g=old?old.gameObject:EchoEnvironmentArt.Node("Anchor_"+id,parent,x,y);g.transform.localPosition=new Vector3(x,y);
                var a=g.GetComponent<EchoAnchor>()??g.AddComponent<EchoAnchor>();a.Id=id;a.Region=r;a.Handoff=note;if(range.HasValue)a.SafeSize=range.Value;
            }
            Add("Surface_WhistleFragment","Surface",-9,30,"S04片段的初始采样位置；B接事件，D供汽笛音频");
            Add("A_MothSpawn","A",12,2,"本地灯尾蛾活动中心；D供实体，B接行为");
            Add("A_OptionalPart","A",34,6,"可选零件在上层箱旁，非强制主线");
            Add("B_Entry","B",45,0,"A-B连续衔接，不切场、不传送");
            Add("B_MothSpawn","B",70,5,"本地蛾群/漏录兜底位置，非固定木偶");
            Add("B_CatchLedge","B",59,-2,"绳桥下方恢复平台");
            Add("D_RecordFallback","D",182,-4,"引右后距177处兽位5单位，B验证采音及安全距离");
            Add("D_ControlBox","D",146,-4,"S02控制盒与录音语义分开，B接有线右喇叭");
            Add("D_OverBeastBridge","D",169,4,"木桥净空，跨越下层小石背兽");
            Add("E_CleanEntry","E",205,-36,"由D自然洞道抵达的干净控制区");
            Add("E_Dust","E",170.5f,-34.5f,"危险范围仅标注：净化门右、控件左；B接伤害，D接尘VFX",new Vector2(12,3));
            foreach(var t in parent.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Ladder_")))
            {
                var box=t.GetComponent<BoxCollider2D>();if(!box)continue;
                var bottom=t.TransformPoint(box.offset-Vector2.up*(box.size.y-.7f)/2);var top=t.TransformPoint(box.offset+Vector2.up*(box.size.y-.7f)/2);
                Add(t.name+"_Foot",t.name.Split('_')[1],bottom.x,bottom.y,"梯脚；几何位置，B接攀爬");Add(t.name+"_Top",t.name.Split('_')[1],top.x,top.y,"梯顶；B验证离梯过渡");
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return ExportAnchors();
        }
        public static string ExportAnchors()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Only export the chapter scene");
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EchoAnchor>(true)).OrderBy(a=>a.Id).ToArray();
            if(all.GroupBy(a=>a.Id).Any(g=>string.IsNullOrWhiteSpace(g.Key)||g.Count()>1))throw new Exception("Empty or duplicate anchor IDs: export refused");
            string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
            var csv=new StringBuilder("ID,区域,X,Y,接入说明\n");
            foreach(var a in all)csv.AppendLine(string.Join(",",Q(a.Id),Q(a.Region),a.transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture),a.transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture),Q(a.Handoff)));
            File.WriteAllText("Docs/EchoCaves/实际场景锚点.csv",csv.ToString(),new UTF8Encoding(true));return "Exported "+all.Length+" native semantic/ladder anchors";
        }
        public static string ExportModules(string folder="Assets/_EchoCaves/Prefabs/Modules-v04")
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=EchoSceneChecks.ScenePath)throw new Exception("Wrong scene");
            if(!folder.StartsWith("Assets/_EchoCaves/Prefabs/"))throw new Exception("Export outside C namespace refused");
            if(Directory.Exists(folder)&&Directory.GetFiles(folder,"*.prefab").Length>0)throw new Exception("Versioned export already exists; choose a new folder, never silently retain stale prefabs");
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();int n=0;
            foreach(var name in new[]{"RopeBridge_B","RopeBridge_C","Device_E_Valve","Device_E_Fan","Device_E_PurificationGate","Device_F_LeftGate","Device_F_Blocker","Device_G_ReturnPower","Device_D_SpeakerRight"})
            {
                var source=GameObject.Find(name);if(!source)throw new Exception("Missing export source "+name);string path=folder+"/"+name+".prefab";
                var preview=EditorSceneManager.NewPreviewScene();GameObject wrapper=null;
                try
                {
                    wrapper=new GameObject(name+"_Module");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wrapper,preview);
                    var b=source.GetComponentsInChildren<Renderer>(true).Select(r=>r.bounds).ToArray();Vector3 pivot=source.transform.position;
                    if(name.StartsWith("RopeBridge")){var edge=source.GetComponent<EdgeCollider2D>();pivot=source.transform.TransformPoint((edge.points.First()+edge.points.Last())/2);}
                    wrapper.transform.position=pivot;var copy=UnityEngine.Object.Instantiate(source);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,preview);copy.transform.SetParent(wrapper.transform,true);wrapper.transform.position=Vector3.zero;
                    PrefabUtility.SaveAsPrefabAsset(wrapper,path,out bool ok);if(!ok)throw new Exception("Save failed "+path);n++;
                }
                finally{if(wrapper)UnityEngine.Object.DestroyImmediate(wrapper);EditorSceneManager.ClosePreviewScene(preview);}
            }
            return "Exported "+n+" normalized reusable environment module prefabs";
        }
    }
}
