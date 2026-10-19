// Обводка «inverted hull»: копия меша, раздутая по нормалям, рисуется только задними гранями.
// Лежит в Resources, чтобы попасть в билд (её создаёт HoverOutline через Shader.Find).
Shader "Hidden/Game/OutlineHull"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.85, 0.25, 1)
        _Width ("Width (meters)", Float) = 0.008
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+10" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                // Раздуваем в мировых метрах, чтобы толщина не зависела от масштаба модели
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                float3 n = normalize(TransformObjectToWorldNormal(i.normalOS));
                o.positionCS = TransformWorldToHClip(ws + n * _Width);
                return o;
            }

            half4 frag (Varyings i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
