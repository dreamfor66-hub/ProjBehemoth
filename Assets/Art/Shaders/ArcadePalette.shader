Shader "Hidden/Behemoth/ArcadePalette"
{
    Properties { _MainTex("Frame",2D)="white"{} }
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
            #include "DarkPalette.hlsl"
            float4 frag(Varyings i):SV_Target
            {
                float3 c=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,i.texcoord).rgb;
                return float4(DarkLinear(DarkMap(DarkSRGB(c),0)),1);
            }
            ENDHLSL
        }
    }
}
