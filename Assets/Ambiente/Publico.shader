// Publico de las tribunas: muchos hinchas combinados en pocas mallas.
// Cada hincha se mueve por su cuenta desde el vertice (uv2.x = fase al azar),
// y con _Emocion alta (gol) todos saltan. La camiseta (color.a = 1) toma el color
// del pais elegido en 6 de cada 10 hinchas (uv2.y), el resto lleva su propio color.
Shader "Penales/Publico"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
        CBUFFER_END
        // globales (las pone el script Publico)
        float4 _ColorHincha;
        float _Emocion;

        float3 Mover(float3 p, float2 uv2)
        {
            float fase = uv2.x * 6.2831;
            float t = _Time.y;
            float salto = abs(sin(t * 7.0 + fase)) * 0.38 * _Emocion;
            float mecer = sin(t * 1.6 + fase) * 0.025;
            p.y += salto + mecer;
            return p;
        }
        ENDHLSL

        Pass
        {
            Name "Publico"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; float4 color : COLOR; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float fog : TEXCOORD1; float hincha : TEXCOORD2; };

            V vert(A i)
            {
                V o;
                float3 p = Mover(i.pos.xyz, i.uv2);
                o.pos = TransformObjectToHClip(p);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.color = i.color;
                o.fog = ComputeFogFactor(o.pos.z);
                o.hincha = i.uv2.y;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                if (i.color.a > 0.5)
                {
                    half3 camiseta = i.hincha < 0.6 ? _ColorHincha.rgb : i.color.rgb;
                    half luz = dot(c.rgb, half3(0.333, 0.333, 0.333));
                    c.rgb = camiseta * (0.8 + 0.3 * luz);
                }
                c.rgb = MixFog(c.rgb, i.fog);
                c.a = 1;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; float2 uv2 : TEXCOORD1; };
            float4 vert(A i) : SV_POSITION { return TransformObjectToHClip(Mover(i.pos.xyz, i.uv2)); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
