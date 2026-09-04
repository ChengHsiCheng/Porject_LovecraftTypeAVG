using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 重影（殘影）的自訂 Volume。
/// intensity 是 0~1 的主控，其餘欄位是「intensity = 1 時」的數值。
/// </summary>
[Serializable]
[VolumeComponentMenu("古神咖啡廳/重影 Ghost")]
public class GhostVolumeComponent : VolumeComponent
{
    [Tooltip("0 = 關閉，1 = 最強")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("每層殘影的偏移距離（像素）。X 是水平、Y 是垂直，可以只開一軸")]
    public Vector2Parameter offset = new Vector2Parameter(new Vector2(12f, 0f));

    [Tooltip("疊幾層殘影")]
    public ClampedIntParameter count = new ClampedIntParameter(1, 1, 4);

    [Tooltip("越後面的殘影越淡，0.6 = 每層再淡 40%")]
    public ClampedFloatParameter falloff = new ClampedFloatParameter(0.6f, 0.1f, 1f);

    [Tooltip("殘影方向繞圈的速度。0 = 固定往 offset 的方向；大於 0 時 X/Y 變成橢圓的兩個半徑")]
    public ClampedFloatParameter driftSpeed = new ClampedFloatParameter(0f, 0f, 5f);

    [Tooltip("殘影的色偏，0 = 跟原圖同色")]
    public ClampedFloatParameter colorShift = new ClampedFloatParameter(0.3f, 0f, 1f);

    public bool IsActive()
    {
        return active && intensity.value > 0f;
    }
}
