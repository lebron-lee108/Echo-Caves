if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play required");
var camera=UnityEngine.Camera.main;
string folder="Evidence/Slice/DynamicPreviewV5";
var bootstrap=UnityEngine.Object.FindFirstObjectByType<VP515.Slice.SliceBootstrap>();bootstrap.UI.MenuRequested();
System.IO.Directory.CreateDirectory(folder);
var rt=new UnityEngine.RenderTexture(1280,720,24);
var texture=new UnityEngine.Texture2D(1280,720,UnityEngine.TextureFormat.RGB24,false);
double start=UnityEditor.EditorApplication.timeSinceStartup,next=start;int index=0;bool journeyStarted=false;
var timestamps=new System.Collections.Generic.List<string>();
UnityEditor.EditorApplication.CallbackFunction capture=null;
capture=()=>{
    try{
        double now=UnityEditor.EditorApplication.timeSinceStartup;
        if(!journeyStarted&&now-start>=1.3){journeyStarted=true;VP515.Editor.SlicePlayTests.Start();}
        if(!UnityEditor.EditorApplication.isPlaying||!camera||now-start>=42){
            UnityEditor.EditorApplication.update-=capture;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
            System.IO.File.WriteAllText(folder+"/timing.txt",string.Join("\n",timestamps));
            System.IO.File.WriteAllText(folder+"/status.txt","Captured "+index+" actual engine frames; approximate12fps; no audio; known-route synthetic input, not human playtesting.");return;
        }
        if(now<next)return;next=now+1.0/12;
        var previous=camera.targetTexture;var active=UnityEngine.RenderTexture.active;
        try{
            camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;
            texture.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);texture.Apply();
            System.IO.File.WriteAllBytes(folder+"/frame-"+index.ToString("D5")+".png",texture.EncodeToPNG());
            timestamps.Add(index+","+(now-start).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));index++;
        }finally{camera.targetTexture=previous;UnityEngine.RenderTexture.active=active;}
    }catch(System.Exception e){UnityEditor.EditorApplication.update-=capture;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);System.IO.File.WriteAllText(folder+"/status.txt","FAILED "+e);}
};
System.IO.File.WriteAllText(folder+"/status.txt","RUNNING fresh42s engine capture");
UnityEditor.EditorApplication.update+=capture;
return "Recording actual engine game+UI at1280x720,42seconds; no audio";
