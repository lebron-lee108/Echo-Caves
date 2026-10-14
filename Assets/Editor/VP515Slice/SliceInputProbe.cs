using UnityEngine;
using UnityEngine.InputSystem;
using VP515.Slice;
namespace VP515.Editor
{
    [DefaultExecutionOrder(10000)]
    public sealed class SliceInputProbe:MonoBehaviour
    {
        SlicePlayer player;int frames;string report="";
        void Start(){player=Object.FindFirstObjectByType<SlicePlayer>();}
        void Update(){
            var k=Keyboard.current;if(k==null)return;
            if(k.spaceKey.isPressed||k.spaceKey.wasPressedThisFrame){
                var type=typeof(SlicePlayer);var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                report+="Space held="+k.spaceKey.isPressed+" edge="+k.spaceKey.wasPressedThisFrame+" ground="+player.IsGrounded+" buffer="+type.GetField("jumpBuffer",flags).GetValue(player)+" coyote="+type.GetField("coyote",flags).GetValue(player)+" y="+player.BodyPosition.y+"\n";
                System.IO.File.WriteAllText("Evidence/Slice/input-probe.txt",report);
            }
            frames++;
        }
    }
}
