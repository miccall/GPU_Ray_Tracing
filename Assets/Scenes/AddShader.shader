Shader "Hidden/AddShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Sample("_Sample",float) = 32 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off 
        ZWrite Off 
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float _Sample;
            float4 frag (v2f i) : SV_Target
            {
                return float4(tex2D(_MainTex, i.uv).rgb, 1.0f / (_Sample + 1.0f));
            }
            
            ENDCG
        }
    }
}
