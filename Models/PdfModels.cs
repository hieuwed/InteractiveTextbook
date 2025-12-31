using System.Windows.Media.Imaging;

namespace InteractiveTextbook.Models;

/// <summary>
/// Đại diện cho một trang PDF
/// </summary>
public class PdfPage
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public BitmapSource? Bitmap { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsLoaded { get; set; }
}

/// <summary>
/// Đại diện cho một tài liệu PDF
/// </summary>
public class PdfDocument
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public DateTime LoadedDate { get; set; }
    public List<PdfPage> Pages { get; set; } = new();
}
