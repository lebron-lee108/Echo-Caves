using UnityEngine;

namespace EchoCaves.Scene
{
    /// <summary>C-owned semantic placement only. B owns gameplay interpretation and persistence.</summary>
    public sealed class EchoAnchor : MonoBehaviour
    {
        public string Id;
        public string Region;
        [TextArea] public string Handoff;
        public Vector2 SafeSize=new Vector2(2,2);
        void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireCube(transform.position,SafeSize);}
    }
}
