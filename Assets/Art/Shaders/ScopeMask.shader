Shader "ThunderVeil/ScopeMask"
{
    // 전체 화면 암막. 스코프 정 중앙은 투명하고, 원형 그라데이션으로 스코프 가장자리까지 어두운 색으로 채움

    Properties
    {
        _ScopeCenter ("Scope Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Inner Radius (frac of height)", Float) = 0.211
        _OuterRadius ("Outer Radius (frac of height)", Float) = 0.264
        _Aspect      ("Aspect (w/h)", Float) = 1.7777
    }
    SubShader
    {
        Tags
        {
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

            // HLSL 시작
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                // Uniforms
                float4 _ScopeCenter;
                float _InnerRadius;
                float _OuterRadius;
                float _Aspect;

                // In Params 구조체
                struct Attributes
                {
                    float4 positionOS : POSITION;
                };

                // Out Params 구조체
                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv         : TEXCOORD0;
                };

                // 버텍스 셰이더
                Varyings vert(Attributes IN)
                {
                    Varyings OUT;

                    // 메쉬 버텍스는 이미 [-1, 1] 사이에 존재하므로 클립 공간에 그대로 건네줌
                    // 카메라 위치에 상관 없이 항상 전체 화면 커버
                    OUT.positionCS = float4(IN.positionOS.xy, 0.0, 1.0);

                    // 화면 좌표 [-1, 1]를 텍스처 좌표 [0, 1]로 변환
                    OUT.uv = IN.positionOS.xy * 0.5 + 0.5;

                    return OUT;
                }

                // 프로그래먼트 셰이더
                half4 frag(Varyings IN) : SV_Target
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