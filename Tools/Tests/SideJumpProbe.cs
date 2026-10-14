VP515.Editor.SlicePlayTests.Stop();
var b=UnityEngine.Object.FindFirstObjectByType<VP515.Slice.SliceBootstrap>();
b.Begin();b.Player.ResetAt(new UnityEngine.Vector2(6.1f,.1f));
var original=UnityEngine.InputSystem.Keyboard.current;
var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("SideJumpProbe");
double start=UnityEditor.EditorApplication.timeSinceStartup;float maxY=-99;
var records=new System.Collections.Generic.List<string>();
System.Action feed=null;UnityEditor.EditorApplication.CallbackFunction sample=null;
feed=()=>{
    double t=UnityEditor.EditorApplication.timeSinceStartup-start;
    keyboard.MakeCurrent();
    var keys=t>.4&&t<.7?new[]{UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.Space}:t>.4&&t<1.2?new[]{UnityEngine.InputSystem.Key.D}:System.Array.Empty<UnityEngine.InputSystem.Key>();
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));
};
sample=()=>{
    double t=UnityEditor.EditorApplication.timeSinceStartup-start;
    maxY=UnityEngine.Mathf.Max(maxY,b.Player.BodyPosition.y);
    if(records.Count==0||t>records.Count*.02)records.Add(t.ToString("F3")+" p="+b.Player.BodyPosition+" v="+b.Player.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity+" can="+b.Player.CanAct+" grounded="+b.Player.IsGrounded);
    if(t>1.5){
        UnityEngine.InputSystem.InputSystem.onBeforeUpdate-=feed;UnityEditor.EditorApplication.update-=sample;
        UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);if(original!=null&&original.added)original.MakeCurrent();
        System.IO.File.WriteAllText("Evidence/Slice/side-jump-probe.txt",string.Join("\n",records)+"\nMAX "+maxY+" FINAL "+b.Player.BodyPosition);
    }
};
UnityEngine.InputSystem.InputSystem.onBeforeUpdate+=feed;UnityEditor.EditorApplication.update+=sample;
return "Observe side-contact jump through real queued key events";
