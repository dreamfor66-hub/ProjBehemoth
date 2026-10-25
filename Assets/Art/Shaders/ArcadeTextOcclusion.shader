Shader "Hidden/Behemoth/Text Occlusion"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend Zero OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Output {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;float4 _Color;
            Output vert(Input i){Output o;o.vertex=UnityObjectToClipPos(i.vertex);o.color=i.color*_Color;o.uv=i.uv;return o;}
            fixed4 frag(Output i):SV_Target{return float4(0,0,0,tex2D(_MainTex,i.uv).a*i.color.a);}
            ENDCG
        }
    }
}
