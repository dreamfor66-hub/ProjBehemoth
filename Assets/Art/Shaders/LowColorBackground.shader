Shader "UI/Behemoth/LowColorBackground"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;float4 _Color;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float3 palette[17]={float3(11,12,22),float3(25,27,45),float3(48,49,72),float3(75,74,96),float3(113,107,117),float3(164,154,145),float3(222,211,175),float3(243,230,191),float3(116,63,57),float3(188,96,70),float3(230,155,80),float3(216,187,98),float3(52,75,67),float3(96,132,91),float3(69,107,130),float3(124,167,163),float3(147,171,182)};
                float4 source=tex2D(_MainTex,i.uv)*i.color;float best=100;float3 result=0;
                [unroll]for(int n=0;n<17;n++)
                {
                    float3 p=palette[n]/255.;
                    #ifndef UNITY_COLORSPACE_GAMMA
                    p=GammaToLinearSpace(p);
                    #endif
                    float d=dot(source.rgb-p,source.rgb-p);if(d<best){best=d;result=p;}
                }
                return float4(result,source.a);
            }
            ENDCG
        }
    }
}
