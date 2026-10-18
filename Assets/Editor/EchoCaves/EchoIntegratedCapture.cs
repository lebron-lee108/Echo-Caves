using System;
using System.IO;
using EchoCaves.Scene;
using EchoCaves.UI;
using UnityEngine;

namespace EchoCaves.Editor
{
    public static class EchoIntegratedCapture
    {
        public static string Capture(string folder="Evidence/EchoCaves/Integrated-v03")
        {
            var c=Camera.main;var ui=UnityEngine.Object.FindFirstObjectByType<EchoUIBindings>();if(!c||!ui)throw new Exception("Open chapter first");
            var canvas=ui.GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldWorld=canvas.worldCamera;var oldPlane=canvas.planeDistance;var oldView=ui.CurrentView;var oldPosition=c.transform.position;
            var oldTarget=c.targetTexture;var oldRT=RenderTexture.active;var layers=UnityEngine.Object.FindObjectsByType<EchoParallaxLayer>(FindObjectsSortMode.None);
            var rt=new RenderTexture(1600,900,24){filterMode=FilterMode.Point};var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
            Directory.CreateDirectory(folder);
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=c;canvas.planeDistance=1;c.targetTexture=rt;
                foreach(EchoUIView view in Enum.GetValues(typeof(EchoUIView)))
                {
                    ui.ShowView((int)view);c.transform.position=view==EchoUIView.MainMenu?new Vector3(-10,6,-20):oldPosition;
                    foreach(var l in layers)l.Apply(c.transform.position);
                    Canvas.ForceUpdateCanvases();foreach(var t in ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false))t.ForceMeshUpdate();Canvas.ForceUpdateCanvases();
                    c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+((int)view).ToString("00")+"-"+view+".png",tex.EncodeToPNG());
                }
                return "13 integrated native Unity UI+environment captures; no simulated gameplay claim";
            }
            finally
            {
                ui.ShowView((int)oldView);canvas.renderMode=oldMode;canvas.worldCamera=oldWorld;canvas.planeDistance=oldPlane;c.targetTexture=oldTarget;c.transform.position=oldPosition;
                foreach(var l in layers)l.Apply(oldPosition);RenderTexture.active=oldRT;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
