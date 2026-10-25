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
            float4 frag(Varyings i):SV_Target{return SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,i.texcoord);}
            ENDHLSL
        }
    }
}
