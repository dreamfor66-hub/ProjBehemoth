Shader "Hidden/Behemoth/Native Text Composite"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha
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
                float4 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,i.texcoord);
                // Keep Korean text at its authored resolution, but remove all soft alpha ramps.
                if(source.a<.5)return 0;
                float3 c=source.rgb/max(source.a,.001);
                return float4(DarkLinear(DarkMap(DarkSRGB(c),0)),1);
            }
            ENDHLSL
        }
    }
}
