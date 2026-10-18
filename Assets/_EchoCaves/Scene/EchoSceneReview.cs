using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoCaves.Scene
{
    /// <summary>Explicit C handoff camera viewer, NOT a player controller. Disable when B supplies camera.</summary>
    public sealed class EchoSceneReview : MonoBehaviour
    {
        public Camera ReviewCamera;
        public Vector2[] Bookmarks;
        public string[] BookmarkNames;
        public bool ReviewInput=true;
        public int CurrentBookmark;
        EchoCaves.UI.EchoUIBindings ui;
        bool menuCamera;
        Vector3 previousPosition;
        void Start(){ui=Object.FindFirstObjectByType<EchoCaves.UI.EchoUIBindings>();}
        public void JumpTo(int index)
        {
            if(!ReviewCamera||Bookmarks==null||index<0||index>=Bookmarks.Length)return;
            CurrentBookmark=index;ReviewCamera.transform.position=new Vector3(Bookmarks[index].x,Bookmarks[index].y,-20);
        }
        void Update()
        {
            if(!ReviewInput||!ReviewCamera||Keyboard.current==null)return;
            // UI arrows navigate controls, never pan the scene behind a modal screen.
            if(!ui)ui=Object.FindFirstObjectByType<EchoCaves.UI.EchoUIBindings>();
            if(ui&&Keyboard.current.f1Key.wasPressedThisFrame)
            {
                var dock=ui.transform.Find("HUD/PreviewNavigation");
                if(dock)dock.gameObject.SetActive(!dock.gameObject.activeSelf);
            }
            bool menu=ui&&(ui.CurrentView==EchoCaves.UI.EchoUIView.MainMenu||ui.CurrentView==EchoCaves.UI.EchoUIView.Settings&&menuCamera);
            if(menu&&!menuCamera){previousPosition=ReviewCamera.transform.position;ReviewCamera.transform.position=new Vector3(-10,6,-20);menuCamera=true;}
            else if(!menu&&menuCamera){ReviewCamera.transform.position=previousPosition;menuCamera=false;}
            if(ui&&ui.CurrentView!=EchoCaves.UI.EchoUIView.HUD)return;
            var k=Keyboard.current;var d=Vector2.zero;
            if(k.leftArrowKey.isPressed)d.x--;if(k.rightArrowKey.isPressed)d.x++;
            if(k.downArrowKey.isPressed)d.y--;if(k.upArrowKey.isPressed)d.y++;
            if(d!=Vector2.zero)ReviewCamera.transform.position+=(Vector3)(d*Time.unscaledDeltaTime*(k.leftShiftKey.isPressed?32:12));
            if(k.pageDownKey.wasPressedThisFrame)JumpTo((CurrentBookmark+1)%Bookmarks.Length);
            if(k.pageUpKey.wasPressedThisFrame)JumpTo((CurrentBookmark+Bookmarks.Length-1)%Bookmarks.Length);
        }
    }
}
