// Additive particles for fire, explosions, sparks, muzzle flashes, lightning, lasers and thruster
// flames (Fire_MAT, ExplosionFire, Sparks, Muzzle_Mat, Lightning_Mat, Thruster_MAT, ...).
// The texture is tinted by the particle's vertex colour and multiplied by _Emission, which lets the
// colour go well past 1 for bloom (the game's materials use 0.2 to 100). The result is also scaled
// by its own alpha (texture alpha x vertex alpha) before it is added to the screen, so the alpha
// fades a particle out without needing an alpha blend.
//
// Rebuilt for URP from the original's single unlit pass. It is forward-only, additive, doesn't write
// depth and has no fog, like the original.
Shader "ZeroGravity/Particles/AdditiveParticleHDR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Emission ("Emission", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            Name "AdditiveParticleHDR"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend One One
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex AdditiveParticleVertex
            #pragma fragment AdditiveParticleFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Emission;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings AdditiveParticleVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float4 AdditiveParticleFragment(Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                // Premultiply by alpha, so that Blend One One fades with it.
                return color.a * (color * _Emission);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
