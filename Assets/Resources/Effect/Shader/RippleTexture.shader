Shader "Custom/RippleTexture" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionCenter ("Distortion Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.1
        _RippleFrequency ("Ripple Frequency", Range(0, 10)) = 5
    }
 
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
 
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
 
            sampler2D _MainTex;
            float2 _DistortionCenter;
            float _DistortionAmount;
            float _RippleFrequency;
 
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
 
            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv - _DistortionCenter.xy;
                float distance = length(uv);
                float ripple = sin(distance * _RippleFrequency - _Time.y * 10);
                float distortion = _DistortionAmount * ripple / (distance + 1);
                float2 offset = distortion * uv;
                return tex2D(_MainTex, i.uv + offset);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
