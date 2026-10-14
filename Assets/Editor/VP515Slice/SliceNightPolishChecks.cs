using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using VP515.Slice;

namespace VP515.Editor
{
    // Narrow prepared-state fixtures, separate from the continuous keyboard journey.
    public static class SliceNightPolishChecks
    {
        static void Call(object target,string method) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        public static string AmbientAndEffectsHaveCleanEnvelopes()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            var loops=b.Sound.GetComponents<AudioSource>().Where(s=>s.loop&&s.clip).ToArray();
            if(loops.Length<2)throw new Exception("FAIL: forest has a melody but no independent wind bed");
            foreach(var source in loops)
            {
                var data=new float[source.clip.samples*source.clip.channels];if(!source.clip.GetData(data,0))throw new Exception("FAIL: cannot read loop samples");
                double energy=0;foreach(float value in data){if(float.IsNaN(value)||Mathf.Abs(value)>.81f)throw new Exception("FAIL: invalid or clipping loop samples");energy+=value*value;}
                if(energy/data.Length<.000001)throw new Exception("FAIL: loop is silent");
                if(Mathf.Abs(data[0]-data[data.Length-1])>.01f)throw new Exception("FAIL: loop boundary clicks");
            }
            foreach(string name in new[]{"step","swing","hit","chime"})
            {
                var clip=(AudioClip)typeof(SliceAudio).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b.Sound);
                var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("FAIL: cannot read "+name+" samples");
                double energy=0;foreach(float value in data){if(float.IsNaN(value)||float.IsInfinity(value)||Mathf.Abs(value)>.81f)throw new Exception("FAIL: invalid or clipping "+name+" samples");energy+=value*value;}
                if(energy/data.Length<.000001)throw new Exception("FAIL: "+name+" is silent");
                if(Mathf.Abs(data[0])>.002f||Mathf.Abs(data[data.Length-1])>.002f)throw new Exception("FAIL: "+name+" begins or ends with an abrupt waveform jump");
            }
            return "PASS: independent ambience and melody are non-silent/bounded with clean loop and effect endpoints; not a listening verdict";
        }
        public static string StartPanelMotionCheck()
        {
            string path="Evidence/Slice/panel-motion.txt";System.IO.Directory.CreateDirectory("Evidence/Slice");System.IO.File.WriteAllText(path,"RUNNING fresh unscaled panel-motion test");
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            RectTransform panel;CanvasGroup group;
            try{
                if(!UnityEditor.EditorApplication.isPlaying||!b)throw new Exception("Play fixture required");
                // Establish a real view transition even when the caller was already paused.
                b.UI.Show(SliceView.Menu);b.UI.Show(SliceView.Paused);
                panel=b.UI.transform.Find("SliceCanvas/PausedPanel").GetComponent<RectTransform>();group=panel.GetComponent<CanvasGroup>();
                if(!group||group.alpha>=.99f)throw new Exception("FAIL: paused panel appears instantly instead of transitioning");
                if(b.Player.CanAct||Time.timeScale!=0)throw new Exception("FAIL: panel transition delays the authoritative pause");
            }catch(Exception e){System.IO.File.WriteAllText(path,"FAIL: "+e.Message);throw;}
            double start=UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.CallbackFunction check=null;
            check=()=>
            {
                if(UnityEditor.EditorApplication.timeSinceStartup-start<.6)return;
                UnityEditor.EditorApplication.update-=check;
                try
                {
                    if(!UnityEditor.EditorApplication.isPlaying||!group)throw new Exception("Session stopped before motion completed");
                    if(group.alpha<.99f||panel.anchoredPosition.sqrMagnitude>.01f)throw new Exception("Unscaled pause-panel motion did not settle");
                    b.UI.Show(SliceView.Paused);
                    if(group.alpha<.99f)throw new Exception("Repeated view synchronization restarts its entrance");
                    if(b.Player.CanAct||Time.timeScale!=0)throw new Exception("Motion advanced the paused game");
                    System.IO.File.WriteAllText(path,"PASS: unscaled panel entrance settles while gameplay stays paused; repeated Show does not restart it");
                }
                catch(Exception e){System.IO.File.WriteAllText(path,"FAIL: "+e.Message);}
                finally{if(b)b.UI.MenuRequested();}
            };
            UnityEditor.EditorApplication.update+=check;
            return "Scheduled actual unscaled panel-motion sampling; inspect panel-motion.txt";
        }
        public static string ResetClearsHurtPresentation()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if(!UnityEditor.EditorApplication.isPlaying)throw new Exception("Play fixture required");
            b.Begin();b.Player.CanAct=true;
            var animator=b.Player.GetComponent<SliceFrameAnimator>();
            try
            {
                b.Player.ReceiveDamage(20,b.Player.BodyPosition+Vector2.left);
                Call(animator,"LateUpdate");
                if(b.Player.BodyRenderer.color.g>=.9f)throw new Exception("Fixture did not produce visible injury");
                b.Player.ResetAt(new Vector2(3,.1f));
                Call(animator,"LateUpdate");
                if(b.Player.BodyRenderer.color.g<.99f || b.Player.BodyRenderer.sprite==b.GirlFrames[7])
                    throw new Exception("FAIL: checkpoint retains the previous red hurt pose after physical reset");
                return "PASS: checkpoint clears visible hurt pose and tint immediately";
            }
            finally{b.Begin();}
        }
        public static string PromptMatchesInteractionReach()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if(!UnityEditor.EditorApplication.isPlaying)throw new Exception("Play fixture required");
            b.Begin();b.Player.CanAct=true;
            try
            {
                b.Player.ResetAt((Vector2)b.Dew.position+Vector2.right*1.27f);
                string dew=(string)b.GetType().GetMethod("Prompt",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);
                if(dew.StartsWith("E"))throw new Exception("FAIL: dew advertises E at 1.27 although collection is out of reach");
                b.Rules.ToggleForm();Call(b.Player,"SyncForm");b.Player.ShellOnPlate=true;
                var flight=b.Player.ActiveTransform.GetComponent<Rigidbody2D>();
                flight.position=(Vector2)b.World.Sigil.position+Vector2.right*1.27f;
                string sigil=(string)b.GetType().GetMethod("Prompt",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);
                if(sigil.StartsWith("E"))throw new Exception("FAIL: sigil advertises E beyond its actual radius");
                flight.position=(Vector2)b.World.Sigil.position+Vector2.right*1.25f;
                string boundary=(string)b.GetType().GetMethod("Prompt",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);
                if(!boundary.StartsWith("E")||!b.World.TryActivateSigil())throw new Exception("FAIL: exact accepted boundary has no actionable prompt");
                return "PASS: dew/sigil prompts match actual reach including the accepted boundary";
            }
            finally{b.Begin();}
        }
    }
}
