using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VP515.Slice;

namespace VP515.Editor
{
    // Queued Input System keyboard events exercise the normal player/UI path.
    // No teleportation or direct gate/enemy progress injection in the journey.
    public static class SlicePlayTests
    {
        static SliceBootstrap b;static Keyboard keyboard,original;static int step;
        static double started,stageStarted,lastStrike;static List<string> report;
        static bool jumpReleased;
        static Key[] wanted=Array.Empty<Key>();static readonly Dictionary<Key,int> pulse=new Dictionary<Key,int>();
        public static string Start()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            Stop();b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if(!b||!b.CameraRig||b.Rules==null)throw new Exception("Slice not initialized.");
            original=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>("SliceTestKeyboard");keyboard.MakeCurrent();
            var gameType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(gameType).Focus();
            b.UI.MenuRequested();report=new List<string>();step=0;started=stageStarted=EditorApplication.timeSinceStartup;lastStrike=0;
            Directory.CreateDirectory("Evidence/Slice");File.WriteAllText("Evidence/Slice/journey.txt","RUNNING new keyboard journey\n");
            InputSystem.onBeforeUpdate+=BeforeInput;EditorApplication.update+=Tick;return "Started queued keyboard full journey; poll Evidence/Slice/journey.txt";
        }
        static void Keys(params Key[] keys)
        {
            wanted=keys;
            foreach(var key in keys)if(key!=Key.A&&key!=Key.D&&key!=Key.W&&key!=Key.S)pulse[key]=2;
        }
        static void BeforeInput()
        {
            if(keyboard==null)return;
            var keys=new List<Key>(wanted);
            foreach(var key in new List<Key>(pulse.Keys)){if(!keys.Contains(key))keys.Add(key);if(--pulse[key]<=0)pulse.Remove(key);}
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
        }
        static void Next(string pass){report.Add("PASS "+pass);step++;stageStarted=EditorApplication.timeSinceStartup;jumpReleased=false;Keys();}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying||!b)throw new Exception("Play session ended during journey.");
                double now=EditorApplication.timeSinceStartup,t=now-stageStarted;
                if(now-started>90||t>18)throw new Exception("Timeout step "+step+" at body="+b.Player.BodyPosition+" active="+b.Player.ActivePosition+" view="+b.UI.CurrentView);
                if(b.UI.CurrentView==SliceView.Dead)throw new Exception("Player died during input journey at "+b.Player.BodyPosition);
                Vector2 p=b.Player.BodyPosition;
                switch(step)
                {
                    case 0:if(t>.4){Keys(Key.Enter);Next("menu has keyboard-selected start");}break;
                    case 1:if(b.UI.CurrentView==SliceView.Playing&&t>.4){Next("Enter starts journey and allows play");}else if(t<.2)Keys(Key.Enter);else Keys();break;
                    case 2:if(p.x<4.7f)Keys(Key.D);else Next("D moves grounded human");break;
                    case 3:if(b.Rules.DewCount==1)Next("E collects one dew");else Keys(Key.E);break;
                    case 4:if(p.x<6.45f)Keys(Key.D);else{Keys(Key.D,Key.Space);Next("approach first stepping stone");}break;
                    case 5:
                        if(t<.35)Keys(Key.D,Key.Space);else Keys(Key.D);
                        if(p.x>9.7f){Keys();Next("Space clears stepping stone");}break;
                    case 6:Keys();if(b.Player.IsGrounded&&p.y<.1f&&t>.2)Next("land before root gap");break;
                    case 7:if(t<.35)Keys(Key.D,Key.Space);else Keys(Key.D);if(p.x>14.1f)Next("jump crosses continuous bridge without teleport");break;
                    case 8:if(t<.35)Keys(Key.D,Key.Space);else Keys(Key.D);if(p.x>18)Next("jump clears landing rock");break;
                    case 9:if(p.x<21.4f)Keys(Key.D);else Next("reach pressure plate");break;
                    case 10:Keys();if(t>.25&&b.Player.ShellOnPlate)Next("body occupies pressure plate");break;
                    case 11:if(b.Rules.Butterfly)Next("Q enters butterfly form");else Keys(Key.Q);break;
                    case 12:if(b.Player.ActivePosition.y<4.05f)Keys(Key.W);else Next("W flies upward while body remains on plate");break;
                    case 13:if(b.Player.ActivePosition.x<28.8f)Keys(Key.D);else Next("butterfly crosses upper-only passage");break;
                    case 14:if(b.Rules.GateUnlocked)Next("E activates sigil using occupied body plate");else Keys(Key.E);break;
                    case 15:if(!b.Rules.Butterfly)Next("Q returns to parked body");else Keys(Key.Q);break;
                    case 16:if(p.x<35.8f)Keys(Key.D);else Next("human passes unlocked gate");break;
                    case 17:
                        if(b.Rules.EnemyDefeated){Next("three real melee attacks defeat creature");break;}
                        float delta=b.Enemy.transform.position.x-p.x;var keys=new List<Key>();
                        if(Mathf.Abs(delta)>1.05f||Mathf.Sign(delta)!=b.Player.Facing)keys.Add(delta>0?Key.D:Key.A);
                        if(now-lastStrike>.5){keys.Add(Key.J);lastStrike=now;}
                        Keys(keys.ToArray());break;
                    case 18:if(p.x<55.4f)Keys(Key.D);else Next("reach exit after combat");break;
                    case 19:if(b.UI.CurrentView==SliceView.Complete)Next("E opens complete screen");else Keys(Key.E);break;
                    case 20:
                        if(!jumpReleased){Keys(Key.Enter);jumpReleased=true;}else Keys();
                        if(b.UI.CurrentView==SliceView.Playing&&t>.3){
                            if(b.Rules.GateUnlocked||b.Rules.EnemyDefeated||b.Rules.DewCount!=0||b.Rules.Health!=100)throw new Exception("Restart leaked progress");
                            if(b.Player.BodyPosition.x>3.5f)throw new Exception("Restart kept exit pose");
                            Next("keyboard replay resets full section");}break;
                    case 21:
                        if(b.UI.CurrentView==SliceView.Paused)Next("Esc opens pause");else Keys(Key.Escape);break;
                    case 22:
                        Keys();if(t>.5){if(b.Player.CanAct||b.Enemy.CanAct||Time.timeScale!=0)throw new Exception("Pause did not stop simulation");Next("pause stops player and enemy");}break;
                    case 23:if(b.UI.CurrentView==SliceView.Playing)Next("Esc resumes");else Keys(Key.Escape);break;
                    case 24:if(b.UI.CurrentView==SliceView.Inventory)Next("Tab opens satchel");else if(t>.3)Keys(Key.Tab);else Keys();break;
                    case 25:Keys();if(t>.3){if(b.Player.CanAct||Time.timeScale!=0)throw new Exception("Inventory failed pause");Next("satchel pauses gameplay");}break;
                    case 26:if(b.UI.CurrentView==SliceView.Playing)Next("Tab closes satchel");else Keys(Key.Tab);break;
                    default:Finish(true);break;
                }
            }
            catch(Exception error){report.Add("FAIL "+error.Message);Finish(false);}
        }
        static void Finish(bool success)
        {
            string text=string.Join("\n",report)+"\nRESULT "+(success?"PASS":"FAIL")+"\nQueued synthetic keyboard, known route; not human playtesting.\n";
            File.WriteAllText("Evidence/Slice/journey.txt",text);Debug.Log(text);Stop();
        }
        public static void Stop(){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=BeforeInput;wanted=Array.Empty<Key>();pulse.Clear();if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}if(original!=null&&original.added)original.MakeCurrent();}
    }
}
