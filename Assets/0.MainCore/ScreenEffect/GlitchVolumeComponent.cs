using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 錯誤（訊號故障）的自訂 Volume。
/// intensity 是 0~1 的主控，其餘欄位是「intensity = 1 時」的數值。
/// </summary>
[Serializable]
[VolumeComponentMenu("古神咖啡廳/錯誤 Glitch")]
public class GlitchVolumeComponent : VolumeComponent
{
    [Tooltip("0 = 關閉，1 = 最強")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("橫向錯位的幅度")]
    public ClampedFloatParameter displacement = new ClampedFloatParameter(0.08f, 0f, 0.3f);

    [Tooltip("橫條的密度，越大條越細")]
    public ClampedFloatParameter blockDensity = new ClampedFloatParameter(24f, 2f, 120f);

    [Tooltip("有多少比例的橫條會錯位")]
    public ClampedFloatParameter blockAmount = new ClampedFloatParameter(0.35f, 0f, 1f);

    [Tooltip("RGB 分離的距離（像素）")]
    public ClampedFloatParameter colorSplit = new ClampedFloatParameter(14f, 0f, 60f);

    [Tooltip("雜訊顆粒的強度")]
    public ClampedFloatParameter noise = new ClampedFloatParameter(0.15f, 0f, 1f);

    [Tooltip("每秒跳動幾次，越大越急促")]
    public ClampedFloatParameter speed = new ClampedFloatParameter(12f, 1f, 60f);

    public bool IsActive()
    {
        return active && intensity.value > 0f;
    }
}
