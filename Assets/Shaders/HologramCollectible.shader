Shader "Custom/HologramCollectible"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.2, 0.9, 1.0, 1.0)
        _ScanlineSpeed ("Scanline Speed", Float) = 4.0
        _ScanlineCount ("Scanline Density", Float) = 28.0
        _GlowIntensity ("Glow Intensity", Range(0.5, 3.0)) = 1.4
        _FlickerSpeed ("Flicker Speed", Float) = 8.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ScanlineSpeed;
            float _ScanlineCount;
            float _GlowIntensity;
            float _FlickerSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 1. Scanline wave
                float scanline = sin((IN.texcoord.y * _ScanlineCount) + (_Time.y * _ScanlineSpeed)) * 0.5 + 0.5;
                scanline = lerp(0.7, 1.3, scanline);

                // 2. Hologram subtle flicker
                float flicker = 0.92 + 0.08 * sin(_Time.y * _FlickerSpeed);

                // 3. Edge glow boost
                float edge = abs(IN.texcoord.x - 0.5) * 2.0;
                float rim = pow(edge, 2.0) * 0.4;

                col.rgb = (col.rgb * scanline + col.rgb * rim) * _GlowIntensity * flicker;
                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
