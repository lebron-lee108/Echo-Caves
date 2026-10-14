using UnityEngine;
namespace VP515.Slice
{
    public sealed class SliceParallax:MonoBehaviour
    {
        public float FollowFactor;
        Camera cameraTarget;Vector3 origin;float cameraOrigin;
        public void Initialize(Camera target){cameraTarget=target;origin=transform.position;cameraOrigin=target.transform.position.x;}
        void Start(){if(!cameraTarget&&Camera.main)Initialize(Camera.main);}
        void LateUpdate(){if(cameraTarget)transform.position=origin+Vector3.right*(cameraTarget.transform.position.x-cameraOrigin)*FollowFactor;}
    }
}
