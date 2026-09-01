using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 暈眩效果的自訂 Volume。
/// intensity 是 0~1 的主控，其餘欄位是「intensity = 1 時」的數值。
/// </summary>
[Serializable]
[VolumeComponentMenu("古神咖啡廳/暈眩 Dizzy")]
public class DizzyVolumeComponent : VolumeComponent
{
    [Tooltip("0 = 關閉，1 = 最強")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("波紋扭曲的幅度")]
    public ClampedFloatParameter distortion = new ClampedFloatParameter(0.03f, 0f, 0.2f);

    [Tooltip("波紋密度，越大波越細")]
    public ClampedFloatParameter waveFrequency = new ClampedFloatParameter(18f, 1f, 60f);

    [Tooltip("波紋往外流動的速度")]
    public ClampedFloatParameter waveSpeed = new ClampedFloatParameter(3f, 0f, 20f);

    [Tooltip("整個畫面來回旋轉的角度（弧度）")]
    public ClampedFloatParameter swirl = new ClampedFloatParameter(0.08f, 0f, 1f);

    [Tooltip("旋轉來回擺動的速度")]
    public ClampedFloatParameter swirlSpeed = new ClampedFloatParameter(1.5f, 0f, 10f);

    [Tooltip("色散，紅藍分離的程度")]
    public ClampedFloatParameter chromatic = new ClampedFloatParameter(1.5f, 0f, 10f);

    [Tooltip("邊緣壓黑的程度")]
    public ClampedFloatParameter vignette = new ClampedFloatParameter(0.9f, 0f, 3f);

    public bool IsActive()
    {
        return active && intensity.value > 0f;
    }
}
