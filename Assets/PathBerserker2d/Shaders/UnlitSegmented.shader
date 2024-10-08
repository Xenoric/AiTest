Shader "Hidden/PB_UnlitSegmented"
{
    Properties
    {
        _SegmentSize ("Segment Size", Float) = 0.14
        _PauseSize ("Pause Size", Float) = 0.14
        _XOffset ("X Offset", Float) = 0
		_Color ("Color", Color) = (0, 0, 1, 1)
	}
    SubShader
    {
		Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            float _SegmentSize;
            float _PauseSize;
            float _XOffset;
            half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float seg = step(frac((i.worldPos.x - i.worldPos.y + _XOffset) / (_SegmentSize + _PauseSize)) * (_SegmentSize + _PauseSize), _SegmentSize);
                half4 c = lerp(half4(0, 0, 0, 0), _Color, seg);
				clip(c.a - 0.5);
				return c;
            }
            ENDCG
        }
    }
}
