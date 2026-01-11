Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _OutlineColor ("Outline Color", Color) = (0,1,0,1) // 기본 초록색
        _OutlineWidth ("Outline Width", Range(0, 10)) = 0
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
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
            fixed4 _OutlineColor;
            float _OutlineWidth;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

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
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 아웃라인 로직: 투명한 픽셀인데, 주변에 불투명한 픽셀이 있으면 색칠함
                if (_OutlineWidth > 0 && c.a == 0)
                {
                    float w = _OutlineWidth * _MainTex_TexelSize.x; 
                    
                    fixed4 pixelUp = tex2D(_MainTex, IN.texcoord + fixed2(0, w));
                    fixed4 pixelDown = tex2D(_MainTex, IN.texcoord - fixed2(0, w));
                    fixed4 pixelRight = tex2D(_MainTex, IN.texcoord + fixed2(w, 0));
                    fixed4 pixelLeft = tex2D(_MainTex, IN.texcoord - fixed2(w, 0));

                    if (pixelUp.a > 0 || pixelDown.a > 0 || pixelRight.a > 0 || pixelLeft.a > 0)
                    {
                        return _OutlineColor; // 아웃라인 색상 반환
                    }
                }

                return c;
            }
            ENDCG
        }
    }
}