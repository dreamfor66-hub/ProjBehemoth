Shader "UI/Behemoth/Ink Surface"
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
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 local:TEXCOORD0; };
            float4 _Color, _ClipRect;
            float _BehemothHideRasterText;
            Output vert(Input i)
            {
                Output o; o.vertex=UnityObjectToClipPos(i.vertex); o.color=i.color*_Color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb=GammaToLinearSpace(o.color.rgb);
                #endif
                o.local=i.vertex.xy; return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                clip(.5-_BehemothHideRasterText);
                // Restrained printed-ink variation, fixed in the panel rather than animated screen noise.
                float grain=frac(sin(dot(floor(i.local*1.3),float2(12.9898,78.233)))*43758.5453);
                i.color.rgb *= .95+grain*.08;
                #ifdef UNITY_UI_CLIP_RECT
                i.color.a *= UnityGet2DClipping(i.local,_ClipRect);
                #endif
                return i.color;
            }
            ENDCG
        }
    }
}
