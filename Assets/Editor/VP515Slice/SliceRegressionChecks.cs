using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VP515.Slice;
namespace VP515.Editor
{
    public static class SliceRegressionChecks
    {
        public static string RejectBrokenBindings()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();var player=b.Player;var walk=b.WalkFrames;float radius=b.World.InteractionRadius;int caught=0;
            try{
                b.Player=null;try{SliceBuilder.Validate();}catch(InvalidOperationException){caught++;}b.Player=player;
                b.WalkFrames=Array.Empty<Sprite>();try{SliceBuilder.Validate();}catch(InvalidOperationException){caught++;}b.WalkFrames=walk;
                b.World.InteractionRadius=float.NaN;try{SliceBuilder.Validate();}catch(InvalidOperationException){caught++;}
            }finally{b.Player=player;b.WalkFrames=walk;b.World.InteractionRadius=radius;}
            if(caught!=3)throw new Exception("FAIL: configuration validator accepted broken player/art/radius; rejected "+caught+"/3");
            return "PASS: validator rejects missing player, empty animation bank and invalid interaction radius";
        }
        public static string ReplayMovesToStart()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();b.Begin();
            b.Player.transform.position=new Vector3(55,.1f,0);b.Player.GetComponent<Rigidbody2D>().position=new Vector2(55,.1f);
            b.UI.Show(SliceView.Complete);b.UI.RestartRequested();
            if(b.Player.BodyTransform.position.x>3.5f)throw new Exception("FAIL: replay leaves the rendered/interpolated body at exit: "+b.Player.BodyTransform.position);
            return "PASS: replay resets physical and rendered player pose";
        }
        public static string CompactHUD()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();b.UI.Show(SliceView.Playing);Canvas.ForceUpdateCanvases();
            var rect=b.UI.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="Vitals");
            if(rect.rect.height>115)throw new Exception("FAIL: vitals panel occupies too much of the scene: "+rect.rect.height);
            var objective=b.UI.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="Objective");
            if(objective.rect.height>90)throw new Exception("FAIL: objective panel exceeds compact target");
            return "PASS: compact HUD geometry leaves the scene open";
        }
        public static string MouseUIAttackBlocked()
        {
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if(!b||!UnityEditor.EditorApplication.isPlaying)throw new Exception("Play fixture required");
            b.UI.Show(SliceView.Playing);
            Canvas.ForceUpdateCanvases();
            var rect=b.UI.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="HudInventoryButton");
            var canvas=rect.GetComponentInParent<Canvas>();
            var point=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            var previous=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>("UIRegressionMouse");mouse.MakeCurrent();
            try{
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});InputSystem.Update();
                bool attack=(bool)typeof(SlicePlayer).GetMethod("Pressed",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"attack"});
                if(attack)throw new Exception("FAIL: pressing HUD satchel is decoded as melee attack");
                return "PASS: HUD press does not decode as melee attack";
            }finally{InputSystem.RemoveDevice(mouse);if(previous!=null&&previous.added)previous.MakeCurrent();}
        }
    }
}
