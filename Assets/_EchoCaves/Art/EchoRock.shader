Shader "EchoCaves/PixelRock"
{
    Properties { _MainTex("Rock",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;half4 _Color;
            CBUFFER_END
            V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv*_MainTex_ST.xy+_MainTex_ST.zw;o.c=i.c;return o;}
            half4 frag(V i):SV_Target{return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_Color*i.c;}
            ENDHLSL
        }
    }
}
