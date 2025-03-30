Shader "Custom/CRTScanlineEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanlineDensity ("Scanline Density", Float) = 400
        _Intensity ("Intensity", Range(0,1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        //LOD 100

        Pass
        {
            Name "CRTScanline"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _ScanlineDensity;
            float _Intensity;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = mul(GetWorldToHClipMatrix(), float4(v.positionOS, 1.0));
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                // Generate scaline effect
                float scanline = sin(uv.y * _ScanlineDensity * 3.1415);
                float brightness = lerp(1.0, 1.0 - _Intensity, scanline * 0.5 + 0.5);

                // Apply to screen texture
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                col.rgb *= brightness;
                //return col;
                return float4(1, 0, 0, 1);
            }

            ENDHLSL
        }
    }
}
