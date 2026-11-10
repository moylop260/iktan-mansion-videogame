// Muestra la textura del reflejo en coordenadas de pantalla (invertida en horizontal): un espejo plano real.
Shader "Mansion/MirrorScreen"
{
    Properties
    {
        _MainTex ("Reflection", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.82, 0.88, 0.92, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Mirror"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                uv.x = 1.0 - uv.x;   // la imagen del reflejo está invertida respecto a lo que se ve en el espejo
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                return half4(c * _Tint.rgb, 1);
            }
            ENDHLSL
        }
    }
}
