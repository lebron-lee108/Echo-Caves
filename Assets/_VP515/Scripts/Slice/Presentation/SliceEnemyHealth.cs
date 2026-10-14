using UnityEngine;
namespace VP515.Slice
{
    public sealed class SliceEnemyHealth:MonoBehaviour
    {
        public SliceRules Rules;public Sprite Quad;public Material Glow;
        readonly SpriteRenderer[] pips=new SpriteRenderer[3];
        void Start()
        {
            for(int i=0;i<3;i++){var go=new GameObject("EnemyHealthPip");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3((i-1)*.22f,1.5f,0);
                go.transform.localScale=new Vector3(.19f,.07f,1);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Quad;sr.material=Glow;sr.sortingOrder=50;pips[i]=sr;}
        }
        void LateUpdate(){for(int i=0;i<3;i++)if(pips[i]){pips[i].enabled=!Rules.EnemyDefeated;pips[i].color=i<Rules.EnemyHealth?new Color(1,.65f,.35f,1):new Color(.3f,.25f,.2f,.45f);}}
    }
}
