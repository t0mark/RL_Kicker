Shader "Custom/UniformMaskUnlit"
{
    Properties{
        _BaseColor ("Base Color", Color) = (0.1,0.4,0.8,1)
        _PatternTex ("Pattern Tex", 2D) = "white" {}
        _MaskTex ("Mask Tex (R=1 apply)", 2D) = "black" {}
        _PatternTiling ("Pattern Tiling", Vector) = (4,4,0,0)
        _PatternStrength ("Pattern Strength", Range(0,1)) = 1
    }
    SubShader{
        Tags{ "RenderType"="Opaque" } LOD 100
        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _BaseColor;
            sampler2D _PatternTex; float4 _PatternTex_ST;
            sampler2D _MaskTex;    float4 _MaskTex_ST;
            float4 _PatternTiling; float _PatternStrength;
            struct appdata{ float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f{ float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 uvMask:TEXCOORD1; };
            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _PatternTiling.xy;
                o.uvMask = TRANSFORM_TEX(v.uv, _MaskTex); return o; }
            fixed4 frag(v2f i):SV_Target{
                fixed3 pat = tex2D(_PatternTex, i.uv).rgb;
                fixed m = tex2D(_MaskTex, i.uvMask).r; // 흑/백 마스크의 R채널
                m = saturate(m * _PatternStrength);
                fixed3 col = lerp(_BaseColor.rgb, pat, m);
                return fixed4(col,1);
            }
            ENDCG
        }
    }
}
