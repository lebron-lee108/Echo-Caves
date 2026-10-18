Shader "EchoCaves/DepthEdgeFade"
{
    Properties { _MainTex("Layer",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) [HideInInspector] _RendererColor("Renderer Color",Color)=(1,1,1,1) }
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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            V vert(A i){SetUpSpriteInstanceProperties();V o;i.p.xyz=UnityFlipSprite(i.p.xyz,unity_SpriteProps.xy);o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c*unity_SpriteColor;return o;}
            half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.c*_Color;float e=saturate(min(i.uv.y,1-i.uv.y)*9);c.a*=floor(e*8)/8;return c;}
            ENDHLSL
        }
    }
}
