Shader "Hidden/Behemoth/ArcadePalette"
{
    Properties
    {
        _MainTex("Frame",2D)="white"{}
        [HideInInspector] _WeakPointStroke("Weak point stroke",Vector)=(0.9098039,0.32941177,0.21568628,1)
        [HideInInspector] _WeakPointShadow("Weak point shadow",Vector)=(0.74509805,0.20784314,0.17254902,1)
        [HideInInspector] _WeakPointCore("Weak point core",Vector)=(0.98039216,0.5921569,0.2901961,1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _WeakPointStroke, _WeakPointShadow, _WeakPointCore;
            float4 frag(Varyings i):SV_Target
            {
                float3 p[17]={float3(11,12,22),float3(25,27,45),float3(48,49,72),float3(75,74,96),float3(113,107,117),float3(164,154,145),float3(222,211,175),float3(243,230,191),float3(116,63,57),float3(188,96,70),float3(230,155,80),float3(216,187,98),float3(52,75,67),float3(96,132,91),float3(69,107,130),float3(124,167,163),float3(147,171,182)};
                float3 c=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,i.texcoord).rgb;float score=100;float3 result=0;
                // Reserve exact UI signal colors without changing the nearest-color mapping of the artwork/HUD.
                float3 signalRed=SRGBToLinear(float3(255,40,40)/255.);
                if(all(abs(c-signalRed)<.003)||all(abs(c-float3(255,40,40)/255.)<.003))return float4(signalRed,1);
                // Exact shared UI colors bypass quantization; the artwork keeps its existing palette.
                float3 weakStroke=SRGBToLinear(_WeakPointStroke.rgb), weakShadow=SRGBToLinear(_WeakPointShadow.rgb), weakCore=SRGBToLinear(_WeakPointCore.rgb);
                if(all(abs(c-weakStroke)<.003)||all(abs(c-_WeakPointStroke.rgb)<.003))return float4(weakStroke,1);
                if(all(abs(c-weakShadow)<.003)||all(abs(c-_WeakPointShadow.rgb)<.003))return float4(weakShadow,1);
                if(all(abs(c-weakCore)<.003)||all(abs(c-_WeakPointCore.rgb)<.003))return float4(weakCore,1);
                if(all(c>.999)||all(c<.00001))return float4(c,1);
                [unroll]for(int n=0;n<17;n++){float3 q=SRGBToLinear(p[n]/255.);float d=dot(c-q,c-q);if(d<score){score=d;result=q;}}
                return float4(result,1);
            }
            ENDHLSL
        }
    }
}
