#ifndef BEHEMOTH_DARK_PALETTE
#define BEHEMOTH_DARK_PALETTE
float4 _BehemothInk0, _BehemothInk1, _BehemothInk2, _BehemothInk3;
float4 _BehemothInk4, _BehemothInk5, _BehemothInk6;
float4 _BehemothValueSteps, _BehemothValues;

float3 DarkSRGB(float3 c)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return c;
    #else
    return lerp(12.92*c,1.055*pow(max(c,0),1./2.4)-.055,step(.0031308,c));
    #endif
}
float3 DarkLinear(float3 c)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return c;
    #else
    return lerp(c/12.92,pow((max(c,0)+.055)/1.055,2.4),step(.04045,c));
    #endif
}
float3 DarkInk(int index)
{
    if(index==0)return _BehemothInk0.rgb;
    if(index==1)return _BehemothInk1.rgb;
    if(index==2)return _BehemothInk2.rgb;
    if(index==3)return _BehemothInk3.rgb;
    if(index==4)return _BehemothInk4.rgb;
    if(index==5)return _BehemothInk5.rgb;
    return _BehemothInk6.rgb;
}
float3 DarkNearest(float3 c)
{
    float best=100;float3 result=0;
    [unroll]for(int n=0;n<7;n++)
    {
        float3 p=DarkInk(n),delta=c-p;
        float score=dot(delta,delta);
        if(score<best){best=score;result=p;}
    }
    return result;
}
// Roles: 0 = UI/composited frame, 1 = architecture, 2 = figure/illustration.
float3 DarkMap(float3 c,int role)
{
    // Saved indexed artwork is idempotent in every role, including dark backgrounds.
    float3 nearest=DarkNearest(c);
    if(all(abs(c-nearest)<.008))return nearest;
    float value=dot(c,float3(.299,.587,.114));
    bool red=c.r>c.g*1.65&&c.r>c.b*1.5&&c.r-c.g>.13&&c.r>.3;
    if(role==1)
    {
        // Only hot cores receive the accent; warm stone never becomes a red carpet.
        if(c.r>c.g*1.07&&c.r>c.b*1.8&&value>.6)return DarkInk(6);
        value*=_BehemothValues.x;
    }
    else
    {
        if(red)return DarkInk(6);
        if(role==2)value*=_BehemothValues.y;
    }
    int tone=value<_BehemothValueSteps.x?0:value<_BehemothValueSteps.y?1:
        value<_BehemothValueSteps.z?2:value<_BehemothValueSteps.w?3:
        value<_BehemothValues.z?4:5;
    return DarkInk(tone);
}
#endif
