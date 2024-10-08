Shader "Hidden/PB_Breakable"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0, 0, 1, 1)
        _BreakTex ("Texture", 2D) = "white" {}
		_BreakProgress("Break Progress", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;

			sampler2D _BreakTex;

			half4 _Tint;
			float _BreakProgress;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

				// sample break texture
				fixed4 br = tex2D(_BreakTex, i.uv);
				int incl = step(br.r - _BreakProgress, 0);
				br = incl * br * col * _Tint;

                return br + _Tint * col * (1- incl);
            }
            ENDCG
        }
    }
}
