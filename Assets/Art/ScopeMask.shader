Shader "ThunderVeil/ScopeMask"
{
    // 전체 화면 암막. 스코프 정 중앙은 투명하고, 원형 그라데이션으로 스코프 가장자리까지 어두운 색으로 채움

    // 셰이더 Uniforms
    Properties
    {
        _ScopeCenter ("Scope Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Inner Radius (frac of height)", Float) = 0.211
        _OuterRadius ("Outer Radius (frac of height)", Float) = 0.264
        _Aspect ("Aspect (w/h)", Float) = 1.7777
    }
    SubShader
    {
        Tags {
            "RenderType"     = "Transparent"        // 투명
            "Queue"          = "Overlay"            // Overlay 큐 사용
            "RenderPipeline" = "UniversalPipeline"  // 공용 Render Pipeline 사용
        }

        Pass // 렌더 패스 설정
        {
            Cull Off                        // 컬링 꺼짐
            ZWrite Off                      // 깊이 마스크 쓰기 꺼짐
            ZTest Always                    // 깊이 테스트
            Blend SrcAlpha OneMinusSrcAlpha // 블렌딩 함수 (src = srcAlpha, dest = 1 - srcAlpha)

            HLSLPROGRAM // hlsl 코드 시작
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Shader Uniforms
            float4 _ScopeCenter;
            float _InnerRadius;
            float _OuterRadius;
            float _Aspect;

            // In Params 구조체
            struct Attributes { float4 positionOS : POSITION; };

            // Out Params 구조체
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // 버텍스 셰이더
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // The mesh verts are already in [-1,1]; emit them straight to clip space
                // so this pass fills the screen regardless of camera/transform.
                OUT.positionCS = float4(IN.positionOS.xy, 0.0, 1.0);
                OUT.uv = IN.positionOS.xy * 0.5 + 0.5;

                return OUT;
            }

            // 프로그래먼트(픽셀) 셰이더
            half4 frag (Varyings IN) : SV_Target
            {
                float2 d = IN.uv - _ScopeCenter.xy;
                d.x *= _Aspect;                       // measure distance in height-normalised units
                float dist = length(d);
                float a = smoothstep(_InnerRadius, _OuterRadius, dist); // 0 in hole -> 1 outside

                return half4(0.0, 0.0, 0.0, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}