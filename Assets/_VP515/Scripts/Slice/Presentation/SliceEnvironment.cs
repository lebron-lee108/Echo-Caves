using UnityEngine;
namespace VP515.Slice
{
    /// <summary>Three depth bands of drifting foliage. Visual-only, independent of progression.</summary>
    public sealed class SliceEnvironment:MonoBehaviour
    {
        public Camera View;public Sprite[] Leaves;
        struct Leaf{public Transform Root;public Vector3 Position;public float Speed,Phase,Depth,Turn;}
        Leaf[] particles;
        float previousCameraX;
        void Start()
        {
            if(!View)View=Camera.main;if(!View||Leaves==null||Leaves.Length==0)return;
            particles=new Leaf[30];var random=new System.Random(515);previousCameraX=View.transform.position.x;
            for(int i=0;i<particles.Length;i++){
                int band=i%3;var root=new GameObject("DriftingLeaf"+i);root.transform.SetParent(transform,false);
                var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=Leaves[i%Leaves.Length];
                renderer.sortingOrder=band==0?-20:band==1?12:36;
                renderer.color=band==0?new Color(.58f,.65f,.7f,.22f):band==1?new Color(.86f,.8f,.65f,.52f):new Color(.55f,.5f,.40f,.42f);
                float size=(band==0?.14f:band==1?.32f:.62f)/renderer.sprite.bounds.size.x;root.transform.localScale=Vector3.one*size;
                var p=new Leaf{Root=root.transform,Position=new Vector3((float)random.NextDouble()*26-13,(float)random.NextDouble()*13-5),Speed=.12f+band*.17f,Depth=band,Phase=(float)random.NextDouble()*6.28f,Turn=(float)random.NextDouble()*80-40};particles[i]=p;
            }
        }
        void LateUpdate()
        {
            if(particles==null)return;float dt=Mathf.Min(Time.unscaledDeltaTime,.05f),time=Time.unscaledTime;
            float cameraDelta=View.transform.position.x-previousCameraX;previousCameraX=View.transform.position.x;
            for(int i=0;i<particles.Length;i++){
                var p=particles[i];p.Position.x-=p.Speed*.6f*dt;p.Position.y-=p.Speed*dt;
                p.Position.x-=cameraDelta*(p.Depth==0?.14f:p.Depth==1?.65f:1.2f);
                if(p.Position.y< -5){p.Position.y=8;p.Position.x=(i*7.13f)%26-13;}
                if(p.Position.x< -13)p.Position.x=13;
                if(p.Position.x>13)p.Position.x=-13;
                p.Root.position=new Vector3(View.transform.position.x+p.Position.x+Mathf.Sin(time*.45f+p.Phase)*(.15f+p.Depth*.1f),p.Position.y,0);
                p.Root.rotation=Quaternion.Euler(0,0,time*p.Turn+Mathf.Sin(time+p.Phase)*30);
                float baseScale=(p.Depth==0?.14f:p.Depth==1?.32f:.62f)/Leaves[i%Leaves.Length].bounds.size.x;
                p.Root.localScale=new Vector3(baseScale*(.45f+.55f*Mathf.Abs(Mathf.Sin(time*.6f+p.Phase))),baseScale,1);
                particles[i]=p;
            }
        }
    }
}
