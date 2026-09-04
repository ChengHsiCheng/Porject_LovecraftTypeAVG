using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 壓黑的自訂 Volume。單純把畫面壓暗，不做任何扭曲或模糊。
/// 跟 @fade:out 的差別：這個是「畫面變暗」，黑幕是「蓋一層不透明色塊」。
/// intensity 是 0~1 的主控，其餘欄位是「intensity = 1 時」的數值。
/// </summary>
[Serializable]
[VolumeComponentMenu("古神咖啡廳/壓黑 Dark")]
public class DarkVolumeComponent : VolumeComponent
{
    [Tooltip("0 = 關閉，1 = 最強")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("整體壓到多暗。1 = 全黑")]
    public ClampedFloatParameter darkness = new ClampedFloatParameter(0.6f, 0f, 1f);

    [Tooltip("邊緣額外壓黑，0 = 整個畫面均勻變暗")]
    public ClampedFloatParameter vignette = new ClampedFloatParameter(0.5f, 0f, 3f);

    [Tooltip("壓黑的顏色。純黑之外也可以壓成暗紅、暗藍")]
    public ColorParameter color = new ColorParameter(Color.black, true, false, true);

    public bool IsActive()
    {
        return active && intensity.value > 0f;
    }
}
