using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Пересчёт позиции курсора с учётом пост-эффектов URP, которые сдвигают картинку
// (Lens Distortion и Panini Projection). Формулы повторяют шейдеры URP:
// UberPost.shader (DistortUV) и PaniniProjection.shader.
// Игрок видит пиксель screenPos, а на самом деле там нарисована точка сцены ToScene(screenPos).
public static class ScreenDistortion {
    public static Vector2 ToScene(Vector2 screenPos, Camera cam) {
        if (cam == null || cam.pixelWidth == 0 || cam.pixelHeight == 0) return screenPos;

        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData != null && !camData.renderPostProcessing) return screenPos;

        VolumeStack stack = camData != null && camData.volumeStack != null ? camData.volumeStack : VolumeManager.instance.stack;
        if (stack == null) return screenPos;

        Rect px = cam.pixelRect;
        Vector2 uv = new((screenPos.x - px.x) / px.width, (screenPos.y - px.y) / px.height);

        // Uber-пасс (Lens Distortion) идёт после Panini, поэтому снимаем эффекты в обратном порядке
        var lens = stack.GetComponent<LensDistortion>();
        if (lens != null && lens.IsActive()) uv = DistortUV(uv, lens);

        var panini = stack.GetComponent<PaniniProjection>();
        if (panini != null && panini.IsActive()) uv = PaniniUV(uv, panini, cam);

        return new Vector2(px.x + uv.x * px.width, px.y + uv.y * px.height);
    }

    // UberPostProcessPass.CalcLensDistortionParams + UberPost.shader DistortUV
    static Vector2 DistortUV(Vector2 uv, LensDistortion ld) {
        float intensity = ld.intensity.value;
        float amount = 1.6f * Mathf.Max(Mathf.Abs(intensity * 100f), 1f);
        float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
        float sigma = 2f * Mathf.Tan(theta * 0.5f);
        Vector2 center = ld.center.value * 2f - Vector2.one;
        Vector2 axis = new(Mathf.Max(ld.xMultiplier.value, 1e-4f), Mathf.Max(ld.yMultiplier.value, 1e-4f));
        float distTheta = intensity >= 0f ? theta : 1f / theta;
        float distScale = 1f / ld.scale.value;

        uv = (uv - new Vector2(0.5f, 0.5f)) * distScale + new Vector2(0.5f, 0.5f);
        Vector2 ruv = Vector2.Scale(axis, uv - new Vector2(0.5f, 0.5f) - center);
        float ru = ruv.magnitude;
        if (ru < 1e-6f) return uv;

        if (intensity > 0f) ru = Mathf.Tan(ru * distTheta) / (ru * sigma);
        else ru = distTheta * Mathf.Atan(ru * sigma) / ru;
        return uv + ruv * (ru - 1f);
    }

    // PaniniProjectionPostProcessPass + PaniniProjection.shader
    static Vector2 PaniniUV(Vector2 uv, PaniniProjection pp, Camera cam) {
        float d = pp.distance.value;
        Vector2 viewExt = ViewExtents(cam.fieldOfView, cam.pixelWidth, cam.pixelHeight);
        Vector2 cropExt = CropExtents(cam.fieldOfView, d, cam.pixelWidth, cam.pixelHeight);
        float scaleF = Mathf.Min(cropExt.x / viewExt.x, cropExt.y / viewExt.y);
        float s = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), pp.cropToFit.value);

        Vector2 viewPos = Vector2.Scale(uv * 2f - Vector2.one, viewExt) * s;
        Vector2 proj = 1f - Mathf.Abs(d) > float.Epsilon ? PaniniGeneric(viewPos, d) : PaniniUnitDistance(viewPos);
        Vector2 ndc = new(proj.x / viewExt.x, proj.y / viewExt.y);
        return ndc * 0.5f + new Vector2(0.5f, 0.5f);
    }

    static Vector2 ViewExtents(float fov, int w, int h) {
        float y = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
        return new Vector2(w / (float)h * y, y);
    }

    static Vector2 CropExtents(float fov, float d, int w, int h) {
        float viewDist = 1f + d;
        Vector2 projPos = ViewExtents(fov, w, h);
        float projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);
        float cylDistMinusD = 1f / projHyp;
        float cylDist = cylDistMinusD + d;
        return projPos * cylDistMinusD * (viewDist / cylDist);
    }

    static Vector2 PaniniUnitDistance(Vector2 v) {
        const float d = 1f, viewDist = 2f, viewDistSq = 4f;
        float viewHyp = Mathf.Sqrt(v.x * v.x + viewDistSq);
        float cylHyp = viewHyp - (v.x * v.x) / viewHyp;
        float frac = cylHyp / viewHyp;
        float cylDist = viewDist * frac;
        return v * frac / (cylDist - d);
    }

    static Vector2 PaniniGeneric(Vector2 v, float d) {
        float viewDist = 1f + d;
        float hypSq = v.x * v.x + viewDist * viewDist;
        float isectD = v.x * d;
        float discrim = hypSq - isectD * isectD;
        float cylDistMinusD = (-isectD * v.x + viewDist * Mathf.Sqrt(Mathf.Max(discrim, 0f))) / hypSq;
        float cylDist = cylDistMinusD + d;
        Vector2 cylPos = v * (cylDist / viewDist);
        return cylPos / (cylDist - d);
    }
}
