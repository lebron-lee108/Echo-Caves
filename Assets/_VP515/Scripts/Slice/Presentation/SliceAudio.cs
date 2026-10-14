using UnityEngine;

namespace VP515.Slice
{
    /// <summary>Original synthesized ambient motif and short feedback; no borrowed audio.</summary>
    public sealed class SliceAudio : MonoBehaviour
    {
        AudioSource music, ambience, effects;
        AudioClip step, swing, hit, chime;
        int footstepIndex;
        public void Initialize()
        {
            music=gameObject.AddComponent<AudioSource>(); effects=gameObject.AddComponent<AudioSource>();
            ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.volume=.24f;ambience.spatialBlend=0;
            var windNoise=new System.Random(515);float windLow=0;
            ambience.clip=Create("Forest wind",24f,t=>{
                float white=(float)windNoise.NextDouble()*2-1;windLow+=.035f*(white-windLow);
                float gust=.55f+.25f*Mathf.Sin(t*.43f)+.15f*Mathf.Sin(t*1.17f);
                float edge=Mathf.Min(1,Mathf.Min(t,24-t)/.6f);
                return (windLow*.48f+white*.012f)*gust*edge;
            });
            music.loop=true; music.volume=.16f; music.spatialBlend=0;
            music.clip=Create("Forest motif",16f,(t)=>{
                float[] notes={146.83f,196f,220f,174.61f,146.83f,261.63f,220f,196f};
                float segment=t%2, f=notes[Mathf.Min(7,(int)(t/2))];
                float envelope=Mathf.Sin(Mathf.PI*segment/2f)*.2f;
                return (Mathf.Sin(t*f*Mathf.PI*2)*.6f+Mathf.Sin(t*f*1.5f*Mathf.PI*2)*.25f)*envelope;
            });
            var effectNoise=new System.Random(1515);float stepLow=0,swingLow=0;
            step=Create("Root and grit footstep",.12f,t=>{float n=(float)effectNoise.NextDouble()*2-1;stepLow+=.22f*(n-stepLow);return (Mathf.Sin(t*75*6.283f)*.22f+stepLow*.25f)*Mathf.Exp(-t*40);});
            swing=Create("Blade air",.18f,t=>{float n=(float)effectNoise.NextDouble()*2-1;swingLow+=.34f*(n-swingLow);return swingLow*.55f*Mathf.Sin(t/.18f*Mathf.PI);});
            hit=Create("Wood impact",.23f,t=>{float n=(float)effectNoise.NextDouble()*2-1;return (Mathf.Sin(t*94*6.283f)+.35f*Mathf.Sin(t*333*6.283f)+n*.8f)*.28f*Mathf.Exp(-t*28);});
            chime=Create("Rune chime",.75f,t=>(Mathf.Sin(t*660*6.283f)+.4f*Mathf.Sin(t*990*6.283f))*.2f*Mathf.Exp(-t*5));
            music.Play();ambience.Play();
        }
        static AudioClip Create(string name,float seconds,System.Func<float,float> sample)
        {
            const int rate=22050; int count=Mathf.CeilToInt(seconds*rate); var data=new float[count];
            for(int i=0;i<count;i++){
                float time=i/(float)rate;
                float edge=Mathf.Clamp01(Mathf.Min(i/(rate*.003f),(count-1-i)/(rate*.008f)));
                data[i]=Mathf.Clamp(sample(time),-.8f,.8f)*edge;
            }
            var clip=AudioClip.Create(name,count,1,rate,false); clip.SetData(data,0); return clip;
        }
        public void Step() { if(effects){effects.pitch=1;effects.PlayOneShot(step,(++footstepIndex%2==0)?.48f:.55f);} }
        public void Swing() { if(effects){effects.pitch=1;effects.PlayOneShot(swing,.65f);} }
        public void Hit() { if(effects){effects.pitch=1;effects.PlayOneShot(hit,.8f);} }
        public void Chime() { if(effects){effects.pitch=1;effects.PlayOneShot(chime,.65f);} }
        void OnDestroy() { if(music&&music.clip) Destroy(music.clip);if(ambience&&ambience.clip)Destroy(ambience.clip); if(step)Destroy(step);if(swing)Destroy(swing);if(hit)Destroy(hit);if(chime)Destroy(chime); }
    }
}
