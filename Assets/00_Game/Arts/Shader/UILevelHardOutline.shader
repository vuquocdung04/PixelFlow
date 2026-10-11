Shader "Custom/UILevelHardOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _OutlineColor ("Outline Color", Color) = (1, 0.1, 0.1, 1)
        _OutlineWidth ("Outline Width (% of size)", Range(0, 0.5)) = 0.15
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 6

        _NoiseScale ("Noise Scale", Range(1, 20)) = 4
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.6
        _NoiseSpeed ("Noise Speed", Range(0, 3)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _PulseSpeed;
            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseSpeed;

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);

                local = local * local * (3.0 - 2.0 * local);

                float2 i0 = fmod(cell, 256.0);
                float2 i1 = fmod(cell + 1.0, 256.0);

                float a = Hash(i0);
                float b = Hash(float2(i1.x, i0.y));
                float c = Hash(float2(i0.x, i1.y));
                float d = Hash(i1);

                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 texel = max(fwidth(IN.texcoord), 1e-6);
                float2 quadPx = 1.0 / texel;
                float2 edge = min(IN.texcoord, 1.0 - IN.texcoord) * quadPx;

                float aspect = texel.y / texel.x;
                float noiseTime = frac(_Time.y * _NoiseSpeed / 256.0) * 256.0;
                float2 noiseUV = float2(IN.texcoord.x * aspect, IN.texcoord.y) * _NoiseScale + noiseTime;

                float noise = Noise(noiseUV);
                float maxWidth = _OutlineWidth * min(quadPx.x, quadPx.y);
                float width = max(maxWidth * (1.0 - _NoiseStrength * noise), 1e-4);

                float dist = min(edge.x, edge.y);
                float border = saturate(1.0 - dist / width);

                border *= border;

                float phase = frac(_Time.y * _PulseSpeed * 0.15915494);
                float pulse = sin(phase * 6.2831853) * 0.5 + 0.5;
                half outline = border * pulse * _OutlineColor.a * IN.color.a;

                color.rgb = lerp(color.rgb, _OutlineColor.rgb, outline);
                color.a = saturate(color.a + outline);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return color;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
