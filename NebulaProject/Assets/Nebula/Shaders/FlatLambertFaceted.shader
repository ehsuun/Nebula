Shader "Nebula/FlatLambertFaceted"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 faceNormal : TEXCOORD0;
            };

            fixed4 _Color;

            v2g vert (appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f output;
                float3 normal = normalize(cross(input[1].worldPos - input[0].worldPos, input[2].worldPos - input[0].worldPos));

                for (int i = 0; i < 3; i++)
                {
                    output.pos = input[i].pos;
                    output.faceNormal = normal;
                    triStream.Append(output);
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // Calculate Lambertian reflectance using face normal
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float lambert = max(0, dot(i.faceNormal, lightDir));

                // Apply flat shading
                return _Color * lambert;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}