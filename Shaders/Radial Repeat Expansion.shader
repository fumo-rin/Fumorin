Shader "Custom/SpriteRadialScroll"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RadialSpeed ("Radial Speed", Float) = 0.5
        _AngularScale ("Angular Scale", Float) = 1
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _RadialSpeed;
            float _AngularScale;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Center UVs
                float2 uv = i.uv - 0.5;

                // Polar coordinates
                float r = length(uv);
                float a = atan2(uv.y, -uv.x);

                // Normalize angle [0,1]
                a = a / (2.0 * UNITY_PI) + 0.5;

                // Radial scroll and angular repetition
                r = frac(r + _RadialSpeed * _Time.y);
                a = frac(a * _AngularScale);

                float2 polarUV = float2(a, r);

                // Sample texture
                fixed4 col = tex2D(_MainTex, polarUV);

                // Optional: fade edges outside 0-1 radius
                if (r > 1.0) col.a = 0;

                // Apply SpriteRenderer color
                col *= _Color;

                return col;
            }
            ENDCG
        }
    }
}
