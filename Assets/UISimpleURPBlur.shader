Shader "UI/SimpleURPBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Blur ("Blur", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _Blur;

            struct Attributes
            {
                float4 positionOS : POSITION;
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
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _Blur;

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + offset);
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - offset);
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(offset.x, -offset.y));
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-offset.x, offset.y));

                return c / 5;
            }
            ENDHLSL
        }
    }
}
