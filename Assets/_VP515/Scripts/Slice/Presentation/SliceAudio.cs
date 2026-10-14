using UnityEngine;

namespace VP515.Slice
{
    /// <summary>Original synthesized ambient motif and short feedback; no borrowed audio.</summary>
    public sealed class SliceAudio : MonoBehaviour
    {
        AudioSource music, effects;
        AudioClip step, swing, hit, chime;
        public void Initialize()
        {
            music=gameObject.AddComponent<AudioSource>(); effects=gameObject.AddComponent<AudioSource>();
            music.loop=true; music.volume=.16f; music.spatialBlend=0;
            music.clip=Create("Forest motif",16f,(t)=>{
                float[] notes={146.83f,196f,220f,174.61f,146.83f,261.63f,220f,196f};
                float segment=t%2, f=notes[Mathf.Min(7,(int)(t/2))];
                float envelope=Mathf.Sin(Mathf.PI*segment/2f)*.2f;
                return (Mathf.Sin(t*f*Mathf.PI*2)*.6f+Mathf.Sin(t*f*1.5f*Mathf.PI*2)*.25f)*envelope;
            });
            step=Create("Soft root footstep",.09f,t=>Mathf.Sin(t*75*6.283f)*Mathf.Exp(-t*48)*.25f);
            swing=Create("Blade air",.16f,t=>(Mathf.Sin(t*1741*6.283f)+Mathf.Sin(t*1189*6.283f))*.1f*Mathf.Sin(t/.16f*Mathf.PI));
            hit=Create("Wood impact",.18f,t=>(Mathf.Sin(t*94*6.283f)+.5f*Mathf.Sin(t*333*6.283f))*.25f*Mathf.Exp(-t*25));
            chime=Create("Rune chime",.75f,t=>(Mathf.Sin(t*660*6.283f)+.4f*Mathf.Sin(t*990*6.283f))*.2f*Mathf.Exp(-t*5));
            music.Play();
        }
        static AudioClip Create(string name,float seconds,System.Func<float,float> sample)
        {
            const int rate=22050; int count=Mathf.CeilToInt(seconds*rate); var data=new float[count];
            for(int i=0;i<count;i++) data[i]=Mathf.Clamp(sample(i/(float)rate),-.8f,.8f);
            var clip=AudioClip.Create(name,count,1,rate,false); clip.SetData(data,0); return clip;
        }
        public void Step() { if(effects) effects.PlayOneShot(step,.5f); }
        public void Swing() { if(effects) effects.PlayOneShot(swing,.65f); }
        public void Hit() { if(effects) effects.PlayOneShot(hit,.8f); }
        public void Chime() { if(effects) effects.PlayOneShot(chime,.65f); }
        void OnDestroy() { if(music&&music.clip) Destroy(music.clip); if(step)Destroy(step);if(swing)Destroy(swing);if(hit)Destroy(hit);if(chime)Destroy(chime); }
    }
}
