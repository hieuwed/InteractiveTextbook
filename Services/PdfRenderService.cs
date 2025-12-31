using System.IO;
using System.Windows.Media.Imaging;
using InteractiveTextbook.Models;
using PDFiumCore;

namespace InteractiveTextbook.Services;

/// <summary>
/// Service quản lý tải và render các trang PDF sử dụng PDFiumCore
/// </summary>
public class PdfRenderService
{
    private PdfDocument? _currentDocument;
    private Dictionary<int, BitmapSource> _pageCache = new();
    private Dictionary<int, System.Runtime.InteropServices.GCHandle> _pageHandles = new();
    private FpdfDocumentT _pdfiumDocument = default!;

    public PdfRenderService()
    {
        try
        {
            // Initialize PDFium
            fpdfview.FPDF_InitLibrary();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDFium init warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Tải tài liệu PDF từ file - đọc số trang thực từ file
    /// </summary>
    public async Task<PdfDocument?> LoadPdfAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"File không tồn tại: {filePath}");
                return null;
            }

            if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"File không phải PDF: {filePath}");
                return null;
            }

            // *** CLEAR TẤT CẢ CACHE VÀ HANDLES CỦA FILE CŨ TRƯỚC KHI TẢI FILE MỚI ***
            ClearCache();

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"File trống: {filePath}");
                return null;
            }

            // Load PDF with PDFiumCore using correct API
            PdfDocument? pdfiumDoc = null;
            
            await Task.Run(() =>
            {
                try
                {
                    // Use fpdfview to load the document
                    _pdfiumDocument = fpdfview.FPDF_LoadDocument(filePath, null);

                    // Get page count from PDF
                    int pageCount = fpdfview.FPDF_GetPageCount(_pdfiumDocument);
                    
                    if (pageCount <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"PDF không có trang: {filePath}");
                        return;
                    }

                    pdfiumDoc = new PdfDocument
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        PageCount = pageCount,  // Số trang thực từ PDF
                        LoadedDate = DateTime.Now
                    };

                    // Khởi tạo danh sách trang
                    for (int i = 0; i < pageCount; i++)
                    {
                        pdfiumDoc.Pages.Add(new PdfPage
                        {
                            PageNumber = i + 1,
                            TotalPages = pageCount,
                            IsLoaded = false
                        });
                    }

                    _currentDocument = pdfiumDoc;
                    System.Diagnostics.Debug.WriteLine($"Tải PDF thành công: {filePath}, {pageCount} trang");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi trong LoadPdfAsync: {ex.Message}");
                }
            });

            return _currentDocument ?? pdfiumDoc;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load PDF: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    public async Task<BitmapSource?> RenderPageAsync(int pageNumber, double scale = 1.0)
    {
        try
        {
            if (_currentDocument == null || pageNumber < 1 || pageNumber > _currentDocument.PageCount)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid page number: {pageNumber}");
                return null;
            }

            // Check cache first
            var cacheKey = pageNumber;
            if (_pageCache.TryGetValue(cacheKey, out var cachedBitmap))
            {
                return cachedBitmap;
            }

            // Render using PDFiumCore
            return await Task.Run(() =>
            {
                FpdfPageT page = default;
                FpdfBitmapT bitmap = default;
                
                try
                {
                    // Load page from document
                    page = fpdfview.FPDF_LoadPage(_pdfiumDocument, pageNumber - 1);
                    System.Diagnostics.Debug.WriteLine($"Loaded page {pageNumber}");

                    // Get page dimensions
                    double pageWidth = fpdfview.FPDF_GetPageWidth(page);
                    double pageHeight = fpdfview.FPDF_GetPageHeight(page);

                    System.Diagnostics.Debug.WriteLine($"Page size: {pageWidth}x{pageHeight}");

                    // Default to reasonable size if dimensions are invalid
                    int width = (int)(pageWidth * scale);
                    int height = (int)(pageHeight * scale);

                    if (width <= 0) width = 600;
                    if (height <= 0) height = 800;

                    System.Diagnostics.Debug.WriteLine($"Rendering page {pageNumber}: {width}x{height}");

                    // Create bitmap using FPDFBitmapCreateEx - use BGRA format (4 = BGRA)
                    bitmap = fpdfview.FPDFBitmapCreateEx(width, height, 4, IntPtr.Zero, 0);
                    System.Diagnostics.Debug.WriteLine($"Bitmap created");

                    // Render PDF page to bitmap
                    fpdfview.FPDF_RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, (int)RenderFlags.RenderAnnotations);
                    System.Diagnostics.Debug.WriteLine($"Page rendered");

                    // Get bitmap buffer
                    var buffer = fpdfview.FPDFBitmapGetBuffer(bitmap);
                    var stride = fpdfview.FPDFBitmapGetStride(bitmap);

                    System.Diagnostics.Debug.WriteLine($"Buffer obtained, stride: {stride}");

                    // Copy buffer data to managed memory before destroying bitmap
                    int bufferSize = stride * height;
                    byte[] managedBuffer = new byte[bufferSize];
                    System.Runtime.InteropServices.Marshal.Copy(buffer, managedBuffer, 0, bufferSize);

                    // Pin the managed buffer so it won't be garbage collected
                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(managedBuffer, System.Runtime.InteropServices.GCHandleType.Pinned);

                    // Store handle for later cleanup
                    _pageHandles[cacheKey] = handle;

                    // Now destroy bitmap and create BitmapSource from managed buffer
                    fpdfview.FPDFBitmapDestroy(bitmap);
                    bitmap = default;

                    // Convert to WPF BitmapSource
                    var source = BitmapSource.Create(
                        width, height, 96, 96,
                        System.Windows.Media.PixelFormats.Bgra32,
                        null,
                        handle.AddrOfPinnedObject(),
                        bufferSize,
                        stride);

                    source.Freeze();

                    // Cache the bitmap
                    _pageCache[cacheKey] = source;

                    System.Diagnostics.Debug.WriteLine($"BitmapSource created and cached for page {pageNumber}");
                    return source;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi render trang {pageNumber}: {ex.Message}\n{ex.StackTrace}");
                    
                    // Cleanup on error
                    try
                    {
                        fpdfview.FPDFBitmapDestroy(bitmap);
                    }
                    catch { }
                    
                    return null;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RenderPageAsync exception: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Pre-render các trang lân cận để cache
    /// </summary>
    public async Task CacheAdjacentPagesAsync(int centerPage)
    {
        if (_currentDocument == null) return;

        var pagesToRender = new List<int>();

        // Pre-render trang trước, hiện tại, và tiếp theo
        if (centerPage > 1) pagesToRender.Add(centerPage - 1);
        pagesToRender.Add(centerPage);
        if (centerPage < _currentDocument.PageCount) pagesToRender.Add(centerPage + 1);

        foreach (var pageNum in pagesToRender)
        {
            if (!_currentDocument.Pages[pageNum - 1].IsLoaded)
            {
                var bitmap = await RenderPageAsync(pageNum, 1.0);
                if (bitmap != null)
                {
                    _currentDocument.Pages[pageNum - 1].Bitmap = bitmap;
                    _currentDocument.Pages[pageNum - 1].IsLoaded = true;
                }
            }
        }
    }

    /// <summary>
    /// Lấy kích thước trang thực từ PDF
    /// </summary>
    public (double Width, double Height) GetPageSize(int pageNumber)
    {
        try
        {
            if (_currentDocument == null || pageNumber < 1 || pageNumber > _currentDocument.PageCount)
                return (600, 800);

            var page = _currentDocument.Pages[pageNumber - 1];
            return (page.Width > 0 ? page.Width : 600, page.Height > 0 ? page.Height : 800);
        }
        catch
        {
            return (600, 800);
        }
    }

    /// <summary>
    /// Clear tất cả cache, handles, và dữ liệu của trang cũ
    /// </summary>
    private void ClearCache()
    {
        try
        {
            // Clear bitmap cache
            foreach (var bitmap in _pageCache.Values)
            {
                if (bitmap != null)
                {
                    (bitmap as BitmapSource)?.Freeze();
                }
            }
            _pageCache.Clear();

            // Clear GCHandles
            foreach (var handle in _pageHandles.Values)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            _pageHandles.Clear();

            // Close old PDFium document
            if (_pdfiumDocument != default)
            {
                try
                {
                    fpdfview.FPDF_CloseDocument(_pdfiumDocument);
                    _pdfiumDocument = default!;
                }
                catch { }
            }

            _currentDocument = null;
            System.Diagnostics.Debug.WriteLine("Cache và handles đã được xóa sạch");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi khi clear cache: {ex.Message}");
        }
    }

    public void Dispose()
    {
        ClearCache();
        _pageCache.Clear();
    }
}
