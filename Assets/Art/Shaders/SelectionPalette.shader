Shader "UI/Behemoth/SelectionPalette"
{
    Properties
    {
        [PerRendererData] _MainTex("Illustration",2D)="white"{}
        _Palette("32 color palette",2D)="white"{}
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            sampler2D _MainTex,_Palette;float4 _Color;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float4 source=tex2D(_MainTex,i.uv);
                float3 backdrop=tex2D(_MainTex,float2(.01,.5)).rgb;
                float3 sampleColor=source.rgb,bg=backdrop;
                #ifndef UNITY_COLORSPACE_GAMMA
                sampleColor=LinearToGammaSpace(sampleColor);bg=LinearToGammaSpace(bg);
                #endif
                float best=100;float3 result=backdrop;
                if(distance(sampleColor,bg)>.085)
                {
                    [unroll]for(int n=0;n<32;n++)
                    {
                        float3 p=tex2D(_Palette,float2((n+.5)/32.,.5)).rgb;
                        float3 compare=p;
                        #ifndef UNITY_COLORSPACE_GAMMA
                        compare=LinearToGammaSpace(compare);
                        #endif
                        float d=dot(sampleColor-compare,sampleColor-compare);
                        if(d<best){best=d;result=p;}
                    }
                }
                return float4(result,source.a)*i.color;
            }
            ENDCG
        }
    }
}
