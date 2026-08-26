Shader "ThunderVeil/Panorama"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _DeltaTime ("Time delta for panorama U texture", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off   // 깊이 버퍼 쓰기 모드 비활성화
            ZTest Always // 깊이 테스트 => 항상 쓰기

            // HLSL 시작
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                // Uniforms
                float _DeltaTime;

                // In params
                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv         : TEXCOORD0;
                };

                // Out params
                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv          : TEXCOORD0;
                };

                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);

                CBUFFER_START(UnityPerMaterial)
                    float4 _BaseMap_ST;
                CBUFFER_END

                // Vertex
                Varyings vert(Attributes IN)
                {
                    Varyings OUT;

                    // 텍스처 U 좌표를 0.75로 압축: 메인 화면 배경의 스프라이트 크기와 같음.
                    IN.uv = float2((IN.uv[0] * 0.75) + _DeltaTime, IN.uv[1]);

                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                    return OUT;
                }

                // Fragment
                half4 frag(Varyings IN) : SV_Target
                {
                    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                    return color;
                }
            ENDHLSL
        }
    }
}