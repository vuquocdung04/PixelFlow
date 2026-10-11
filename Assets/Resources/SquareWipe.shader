Shader "Custom/SquareWipe"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _IsInvert ("Invert", Float) = 0
        _ColorTop ("Color Top", Color) = (0.003921569, 0.5529412, 1, 1)
        _ColorBottom ("Color Bottom", Color) = (0.4745098, 0.9568628, 0.9764706, 1)
        _Grid ("Grid (cols, 1/cols, delayScale, jitter)", Vector) = (9, 0.1111111, 0.57, 0.08)
        _InvSpread ("Inverse Spread", Float) = 2.857143
        _Feather ("Feather", Range(0, 0.05)) = 0.004
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 grid   : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            fixed4 _ColorTop;
            fixed4 _ColorBottom;
            half   _Progress;
            half   _IsInvert;
            float4 _Grid;
            half   _InvSpread;
            half   _Feather;

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);

                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                OUT.grid  = float2(IN.texcoord.x, (IN.texcoord.y - 0.5) / aspect) * _Grid.x;
                OUT.color = lerp(_ColorBottom, _ColorTop, IN.texcoord.y);
                OUT.color.a *= IN.color.a;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 cell  = floor(IN.grid);
                half2  local = (half2)(IN.grid - cell) - 0.5h;

                half x = saturate((cell.x + 0.5) * _Grid.y);
                x = lerp(x, 1.0h - x, _IsInvert);

                half delay = x * _Grid.z + frac(cell.y * 0.618034) * _Grid.w;
                half p     = saturate((_Progress - delay) * _InvSpread);
                half size  = lerp(p, 1.0h - p, _IsInvert);

                half halfSize = size * (0.5h + _Feather * 2.0h);
                half dist     = max(abs(local.x), abs(local.y));
                half inside   = (1.0h - smoothstep(halfSize - _Feather, halfSize + _Feather, dist)) * step(0.0001h, size);

                fixed4 col = IN.color;
                col.a *= inside;

                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
