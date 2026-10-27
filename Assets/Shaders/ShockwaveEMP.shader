Shader "Custom/ShockwaveEMP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Pulse Color", Color) = (0.3, 0.9, 1.0, 1.0)
        _RingRadius ("Ring Radius", Range(0.0, 1.0)) = 0.5
        _RingThickness ("Ring Thickness", Range(0.01, 0.4)) = 0.08
        _GlowPower ("Glow Power", Range(1.0, 5.0)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _RingRadius;
            float _RingThickness;
            float _GlowPower;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 centerUV = IN.texcoord - float2(0.5, 0.5);
                float dist = length(centerUV) * 2.0;

                float ringDist = abs(dist - _RingRadius);
                float alpha = saturate(1.0 - (ringDist / _RingThickness));
                alpha = pow(alpha, 1.5);

                fixed4 col = IN.color;
                col.rgb *= _GlowPower;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
