Shader "Jerry/World Steel Texture"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TileSize ("Fixed world tile size", Vector) = (28.43,16,0,0)
        _WallOrigin ("Wall origin", Vector) = (0,0,0,0)
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; UNITY_SKINNED_VERTEX_INPUTS };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; float2 wallPosition : TEXCOORD4; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _TileSize;
                float4 _WallOrigin;
            CBUFFER_END
            Varyings GlowVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.wallPosition = TransformObjectToWorld(input.positionOS).xy;
                return output;
            }
            half4 GlowFragment(Varyings input) : SV_Target
            {
                float2 uv = (input.wallPosition - _WallOrigin.xy) / _TileSize.xy + .5;
                // Mirrored repetition keeps the existing non-tileable art continuous.
                uv = 1 - abs(frac(uv * .5) * 2 - 1);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * input.color;
            }
            ENDHLSL
        }
    }
}
