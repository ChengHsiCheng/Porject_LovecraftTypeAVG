using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 夢境的自訂 Volume。柔焦、光暈、褪色、緩慢呼吸。
/// intensity 是 0~1 的主控，其餘欄位是「intensity = 1 時」的數值。
/// </summary>
[Serializable]
[VolumeComponentMenu("古神咖啡廳/夢境 Dream")]
public class DreamVolumeComponent : VolumeComponent
{
    [Tooltip("0 = 關閉，1 = 最強")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("柔焦的擴散半徑（像素）")]
    public ClampedFloatParameter blur = new ClampedFloatParameter(6f, 0f, 30f);

    [Tooltip("亮部的光暈強度，讓畫面像隔著一層霧發光")]
    public ClampedFloatParameter glow = new ClampedFloatParameter(0.5f, 0f, 1.5f);

    [Tooltip("亮部從多亮開始發光，越低整體越糊")]
    public ClampedFloatParameter glowThreshold = new ClampedFloatParameter(0.6f, 0f, 1f);

    [Tooltip("褪色程度，1 = 完全灰階")]
    public ClampedFloatParameter desaturate = new ClampedFloatParameter(0.35f, 0f, 1f);

    [Tooltip("整體色調，夢境通常偏冷或偏暖")]
    public ColorParameter tint = new ColorParameter(new Color(0.85f, 0.9f, 1f, 1f), true, false, true);

    [Tooltip("邊緣暈開的程度")]
    public ClampedFloatParameter edgeSoftness = new ClampedFloatParameter(0.6f, 0f, 2f);

    [Tooltip("呼吸般的明暗起伏速度，0 = 不呼吸")]
    public ClampedFloatParameter breathSpeed = new ClampedFloatParameter(0.6f, 0f, 4f);

    public bool IsActive()
    {
        return active && intensity.value > 0f;
    }
}
