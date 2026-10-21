#ifndef PORTAL_GATEWAY_NODES_INCLUDED
#define PORTAL_GATEWAY_NODES_INCLUDED

float PortalHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float PortalValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = PortalHash21(i);
    float b = PortalHash21(i + float2(1.0, 0.0));
    float c = PortalHash21(i + float2(0.0, 1.0));
    float d = PortalHash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float PortalFbmNoise(float2 p)
{
    float v = 0.0;
    float amp = 0.5;
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        v += amp * PortalValueNoise(p);
        p = p * 2.02 + float2(13.1, 7.7);
        amp *= 0.5;
    }
    return v;
}

float EvaluatePortalBaseShape(float2 uv, float3 normalWS, float3 viewToCamera, float4 maskSample, int mode)
{
    float2 p = (uv - 0.5) * 2.0;

    if (mode == 5)
    {
        float ndotv = abs(dot(SafeNormalize(normalWS), SafeNormalize(viewToCamera)));
        return saturate(pow(ndotv, 0.75));
    }

    if (mode == 0)
    {
        return saturate(1.0 - length(p));
    }
    else if (mode == 1)
    {
        float dTop = length(p) / 0.88;
        float dSide = abs(p.x) / 0.88;
        float dBot = max(0.0, (-p.y - 0.82) / 0.16);
        float dist = (p.y > 0.0) ? dTop : max(dSide, dBot);
        return saturate(1.0 - dist);
    }
    else if (mode == 2)
    {
        float widthAtY = max(0.05, 1.0 - p.y * p.y * 0.85);
        float dx = p.x / (0.68 * widthAtY);
        float dist = sqrt(dx * dx + p.y * p.y);
        return saturate(1.0 - dist);
    }
    else if (mode == 3)
    {
        float dist = abs(p.x) * 1.15 + abs(p.y) * 0.95;
        return saturate(1.0 - dist);
    }
    else
    {
        float maskVal = max(maskSample.r, maskSample.a);
        float centerWeight = saturate(1.15 - length(p) * 0.85);
        return maskVal * centerWeight;
    }
}

void EvaluatePortalShapeAndLens_float(
    float2 UV,
    float3 NormalWS,
    float3 PositionWS,
    float4 ScreenPos,
    float4 MaskSample,
    float ShapeMode,
    float OpenProgress,
    float NoiseStrength,
    float NoiseScale,
    float SwirlStrength,
    float LensCurvature,
    float EdgePower,
    float RimWidth,
    float RippleStrength,
    float RippleSpeed,
    out float2 PortalUV,
    out float Fresnel,
    out float RimMask,
    out float LensEdge,
    out float ClipAlpha)
{
    int mode = (int)round(ShapeMode);
    float3 viewToCamera = SafeNormalize(_WorldSpaceCameraPos - PositionWS);
    float baseShape = EvaluatePortalBaseShape(UV, NormalWS, viewToCamera, MaskSample, mode);

    float2 lensPlane = (UV - 0.5) * 2.0;
    float r = length(lensPlane);
    float angle = atan2(lensPlane.y, lensPlane.x);
    float time = _Time.y * RippleSpeed;

    float swirlAngle = angle + (1.0 - baseShape) * SwirlStrength - time * 0.8;
    float2 swirlUV = float2(cos(swirlAngle), sin(swirlAngle)) * r * NoiseScale + float2(0.0, -time * 0.4);
    float noise = PortalFbmNoise(swirlUV);

    float transitionBoost = sin(saturate(OpenProgress) * 3.14159265);
    float effectiveNoise = (noise - 0.5) * NoiseStrength * (0.25 + 0.75 * transitionBoost);
    float shapeField = saturate(baseShape + effectiveNoise);

    float threshold = lerp(1.02, 0.02, saturate(OpenProgress));
    float delta = shapeField - threshold;

    bool visible = (OpenProgress > 0.001) && (baseShape > 0.001) && (delta >= 0.0);
    ClipAlpha = visible ? 1.0 : 0.0;

    float sphereZ = sqrt(saturate(1.0 - dot(lensPlane, lensPlane)));
    float3 virtualSphereNormalOS = normalize(float3(lensPlane * max(0.01, LensCurvature), sphereZ));

    float3 normal = (mode == 5)
        ? SafeNormalize(NormalWS)
        : SafeNormalize(TransformObjectToWorldNormal(virtualSphereNormalOS));

    if (dot(normal, viewToCamera) < 0.0)
        normal *= -1.0;

    Fresnel = pow(1.0 - saturate(dot(normal, viewToCamera)), max(1.0, EdgePower));
    RimMask = 1.0 - smoothstep(0.0, max(0.001, RimWidth), max(0.0, delta));
    LensEdge = 1.0 - saturate(max(0.0, delta) / max(RimWidth * 3.0, 0.001));

    float2 rippleOffset = float2(
        sin(UV.y * 16.0 + time * 1.6) + cos(UV.x * 12.0 - time * 1.2),
        cos(UV.x * 16.0 - time * 1.4) + sin(UV.y * 12.0 + time * 1.8)
    ) * RippleStrength * (0.3 + 1.5 * transitionBoost);

    float2 lensBendOffset = lensPlane * (1.0 - sphereZ) * LensCurvature * 0.06;
    PortalUV = clamp(ScreenPos.xy + rippleOffset - lensBendOffset, 0.001, 0.999);
}

void EvaluatePortalShapeAndLens_half(
    half2 UV,
    half3 NormalWS,
    half3 PositionWS,
    half4 ScreenPos,
    half4 MaskSample,
    half ShapeMode,
    half OpenProgress,
    half NoiseStrength,
    half NoiseScale,
    half SwirlStrength,
    half LensCurvature,
    half EdgePower,
    half RimWidth,
    half RippleStrength,
    half RippleSpeed,
    out half2 PortalUV,
    out half Fresnel,
    out half RimMask,
    out half LensEdge,
    out half ClipAlpha)
{
    float2 portalUV_f;
    float fresnel_f, rimMask_f, lensEdge_f, clipAlpha_f;

    EvaluatePortalShapeAndLens_float(
        UV, NormalWS, PositionWS, ScreenPos, MaskSample,
        ShapeMode, OpenProgress, NoiseStrength, NoiseScale, SwirlStrength,
        LensCurvature, EdgePower, RimWidth, RippleStrength, RippleSpeed,
        portalUV_f, fresnel_f, rimMask_f, lensEdge_f, clipAlpha_f);

    PortalUV = (half2)portalUV_f;
    Fresnel = (half)fresnel_f;
    RimMask = (half)rimMask_f;
    LensEdge = (half)lensEdge_f;
    ClipAlpha = (half)clipAlpha_f;
}

void ComposePortalColor_float(
    float4 PortalTexSample,
    float4 FallbackColor,
    float4 InnerGlowColor,
    float4 RimColor,
    float HasPortalTex,
    float TargetBrightness,
    float Fresnel,
    float RimMask,
    float LensEdge,
    out float3 FinalColor)
{
    float3 portalColor;
#ifdef SHADERGRAPH_PREVIEW
    portalColor = lerp(FallbackColor.rgb, InnerGlowColor.rgb, saturate(Fresnel * 0.75 + (1.0 - LensEdge) * 0.25));
#else
    if (HasPortalTex > 0.5)
    {
        portalColor = PortalTexSample.rgb * TargetBrightness;
    }
    else
    {
        portalColor = lerp(FallbackColor.rgb, InnerGlowColor.rgb, Fresnel * 0.6);
    }
#endif

    float3 color = portalColor * lerp(1.04, 0.88, LensEdge);
    color += InnerGlowColor.rgb * (Fresnel * 0.45 + LensEdge * 0.25);
    color = lerp(color, RimColor.rgb, RimMask * RimMask);
    FinalColor = color;
}

void ComposePortalColor_half(
    half4 PortalTexSample,
    half4 FallbackColor,
    half4 InnerGlowColor,
    half4 RimColor,
    half HasPortalTex,
    half TargetBrightness,
    half Fresnel,
    half RimMask,
    half LensEdge,
    out half3 FinalColor)
{
    float3 finalColor_f;
    ComposePortalColor_float(
        PortalTexSample, FallbackColor, InnerGlowColor, RimColor,
        HasPortalTex, TargetBrightness, Fresnel, RimMask, LensEdge,
        finalColor_f);
    FinalColor = (half3)finalColor_f;
}

#endif
