Shader "ThunderVeil/ScopeMask"
{
    // Fullscreen screen-space "night" mask: opaque black everywhere except a soft
    // circular hole centred on the scope. Replaces the finite vignette sprite so the
    // darkness always covers the whole screen no matter where the scope moves.
    Properties
    {
        _ScopeCenter ("Scope Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Inner Radius (frac of height)", Float) = 0.211
        _OuterRadius ("Outer Radius (frac of height)", Float) = 0.264
        _Aspect ("Aspect (w/h)", Float) = 1.7777
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _ScopeCenter;
            float _InnerRadius;
            float _OuterRadius;
            float _Aspect;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                // The mesh verts are already in [-1,1]; emit them straight to clip space
                // so this pass fills the screen regardless of camera/transform.
                OUT.positionCS = float4(IN.positionOS.xy, 0.0, 1.0);
                OUT.uv = IN.positionOS.xy * 0.5 + 0.5;
                return OUT;
            }

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
