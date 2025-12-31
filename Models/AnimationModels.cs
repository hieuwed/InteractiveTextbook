namespace InteractiveTextbook.Models;

/// <summary>
/// Trạng thái động ảnh lật trang
/// </summary>
public class PageFlipState
{
    /// <summary>
    /// Độ tiến triển của lật trang (0-1)
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Trang hiện tại đang được lật
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Trang tiếp theo (sau khi lật)
    /// </summary>
    public int NextPage { get; set; }

    /// <summary>
    /// Vị trí chuột X tính từ lúc bắt đầu lật
    /// </summary>
    public double MouseDeltaX { get; set; }

    /// <summary>
    /// Là lật tiến (sang trang kế) hay lật lùi
    /// </summary>
    public bool IsFlippingForward { get; set; }

    /// <summary>
    /// Vận tốc lật (dùng cho animation sau khi thả chuột)
    /// </summary>
    public double FlipVelocity { get; set; }

    /// <summary>
    /// Xoay quanh cạnh phải hay cạnh trái
    /// </summary>
    public bool FlipFromRight { get; set; }
}

/// <summary>
/// Trạng thái zoom
/// </summary>
public class ZoomState
{
    public double ZoomLevel { get; set; } = 1.0;
    public double MinZoom { get; set; } = 0.5;
    public double MaxZoom { get; set; } = 3.0;
    public bool FitToWidth { get; set; }
    public bool FitToHeight { get; set; }
}
