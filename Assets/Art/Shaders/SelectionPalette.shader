Shader "UI/Behemoth/SelectionPalette"
{
    Properties
    {
        [PerRendererData] _MainTex("Illustration",2D)="white"{}
        [HideInInspector] _Palette("Legacy palette",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "DarkPalette.hlsl"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;float4 _Color;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float4 source=tex2D(_MainTex,i.uv);
                float3 c=DarkSRGB(source.rgb),bg=DarkSRGB(tex2D(_MainTex,float2(.01,.5)).rgb);
                float3 result=distance(c,bg)<.13?DarkInk(0):DarkMap(c,2);
                return float4(DarkLinear(result),step(.5,source.a*i.color.a));
            }
            ENDCG
        }
    }
}
