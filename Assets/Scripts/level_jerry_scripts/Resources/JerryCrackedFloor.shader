Shader "Jerry/Cracked Floor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _CrackColor ("Crack Color", Color) = (0.12, 0.09, 0.08, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 local : TEXCOORD1; half4 color : COLOR; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _CrackColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.local = input.positionOS.xy + 0.5;
                output.color = input.color;
                return output;
            }
            float Segment(float2 p, float2 a, float2 b)
            {
                float2 direction = b - a;
                float t = saturate(dot(p - a, direction) / dot(direction, direction));
                // The floor is five times wider than it is tall.
                return length((p - a - t * direction) * float2(5, 1));
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 p = input.local;
                float d = Segment(p, float2(.24, 1), float2(.32, .64));
                d = min(d, Segment(p, float2(.32, .64), float2(.27, .37)));
                d = min(d, Segment(p, float2(.27, .37), float2(.38, 0)));
                d = min(d, Segment(p, float2(.32, .64), float2(.46, .8)));
                d = min(d, Segment(p, float2(.27, .37), float2(.12, .22)));
                d = min(d, Segment(p, float2(.76, 1), float2(.69, .7)));
                d = min(d, Segment(p, float2(.69, .7), float2(.75, .4)));
                d = min(d, Segment(p, float2(.75, .4), float2(.65, 0)));
                d = min(d, Segment(p, float2(.75, .4), float2(.9, .25)));
                d = min(d, Segment(p, float2(.69, .7), float2(.57, .6)));
                float crack = 1 - smoothstep(.012, .028, d);
                color.rgb = lerp(color.rgb, _CrackColor.rgb, crack);
                return color;
            }
            ENDHLSL
        }
    }
}
