using UnityEngine;

namespace EchoCaves.Scene
{
    /// <summary>Visual presentation only; does not control the gameplay camera.</summary>
    public sealed class EchoParallaxLayer : MonoBehaviour
    {
        public Camera TargetCamera;
        [Range(0,1.4f)] public float RelativeSpeed=.2f;
        public Vector3 ReferenceCamera;
        public Vector3 ReferencePosition;
        public bool VerticalParallax;
        public void Apply(Vector3 cameraPosition)
        {
            var delta=cameraPosition-ReferenceCamera;delta.z=0;if(!VerticalParallax)delta.y=0;
            transform.position=ReferencePosition+delta*(1-RelativeSpeed);
        }
        void LateUpdate(){if(TargetCamera)Apply(TargetCamera.transform.position);}
    }
}
