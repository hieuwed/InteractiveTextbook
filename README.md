# Interactive Textbook Viewer

Ứng dụng xem PDF tương tác với hiệu ứng lật trang mượt mà giống lật sách thực tế.

## Tính năng

✨ **Hiệu ứng lật trang 3D mượt mà** - Giống lật sách thực tế với physics-based animation
📄 **Hỗ trợ PDF** - Load và xem các file PDF
🔍 **Zoom và Pan** - Phóng to/thu nhỏ tài liệu
⌨️ **Điều hướng** - Nút lật trang trước/sau
💾 **Cache thông minh** - Pre-render các trang lân cận để hiệu suất cao

## Yêu cầu hệ thống

- .NET 8.0 trở lên
- Windows 7 trở lên (với WPF support)
- Visual Studio 2022 (để phát triển)

## Cài đặt

1. Clone repository
```bash
git clone <repo-url>
cd InteractiveTextbook
```

2. Build project
```bash
dotnet build
```

3. Chạy ứng dụng
```bash
dotnet run
```

## Cách sử dụng

### Tải PDF
1. Click nút "📂 Mở PDF"
2. Chọn file PDF từ máy tính
3. Tài liệu sẽ tải và hiển thị

### Lật trang
- **Chuột**: Kéo từ phải sang trái để lật trang sau, hoặc từ trái sang phải để lật trang trước
- **Nút**: Click "Trang sau ▶" hoặc "◀ Trang trước" để lật tự động

### Zoom
- **Nút 🔍+**: Phóng to
- **Nút 🔍-**: Thu nhỏ
- **Nút Bình thường**: Reset zoom về 100%

## Kiến trúc

```
InteractiveTextbook/
├── Models/                 # Data models (PdfDocument, PageFlipState, etc.)
├── Services/              # PDF rendering service
├── ViewModels/            # MVVM ViewModels
├── Views/                 # XAML UI + code-behind
├── Animations/            # Page flip animation engine + 3D renderer
└── Properties/            # Assembly info
```

### Thành phần chính

#### 1. **PdfRenderService** (`Services/PdfRenderService.cs`)
- Load PDF files sử dụng PDFiumCore
- Render trang thành Bitmap
- Hỗ trợ caching các trang lân cận

#### 2. **PageFlipAnimationEngine** (`Animations/PageFlipAnimationEngine.cs`)
- Engine tạo hiệu ứng lật trang
- Physics-based animation với friction và momentum
- Xử lý mouse drag và auto-flip

#### 3. **PageFlip3DRenderer** (`Animations/PageFlip3DRenderer.cs`)
- Tính toán transformations 3D
- Tạo shadow và gradient lighting
- Clip geometry cho phần trang đã lật

#### 4. **PageFlipControl** (`Views/PageFlipControl.xaml.cs`)
- Custom control render hiệu ứng lật trang
- Xử lý mouse events
- Trigger re-render với 60 FPS khi animating

## Tuning và Tối ưu hóa

### Hiệu ứng lật trang
Chỉnh các hằng số trong `PageFlipAnimationEngine.cs`:
```csharp
private const double FRICTION = 0.95;  // Hệ số ma sát (↓ = faster decay)
private const double MIN_VELOCITY = 0.1; // Vận tốc tối thiểu
private const double FLIP_COMPLETE_THRESHOLD = 0.95; // Ngưỡng hoàn thành
```

### Chất lượng render
Điều chỉnh trong `PdfRenderService.cs`:
```csharp
// Thay đổi DPI hoặc scale factor khi render
var bitmap = await RenderPageAsync(pageNumber, scale: 1.5); // 150% chất lượng
```

### Performance
- Increase cache size trong `CacheAdjacentPagesAsync()`
- Sử dụng thread pool cho render operations
- Profile với built-in VS performance tools

## Tích hợp với C# .NET 8 app

```csharp
// Trong host application
var viewer = new InteractiveTextbook.Views.MainWindow();
viewer.Show();
```

Hoặc tạo thư viện:
```csharp
// ViewerLibrary.cs
public class TextbookViewer
{
    private readonly PdfRenderService _pdfService = new();
    
    public async Task<Bitmap> LoadPageAsBitmapAsync(string filePath, int page)
    {
        var doc = await _pdfService.LoadPdfAsync(filePath);
        return await _pdfService.RenderPageAsync(page);
    }
}
```

## Troubleshooting

### Lỗi: "PDFiumCore not loaded"
- Cài đặt NuGet package mới nhất
- Kiểm tra bitness (32-bit vs 64-bit) của project

### Hiệu ứng lật chậm/giật
- Giảm resolution (scale factor) khi render
- Tắt shadow/gradient effects
- Check taskbar performance (GPU usage)

### Crash khi load PDF
- Kiểm tra file PDF có bị hỏng không
- Thử extract pages từ PDF khác
- Check memory usage (large PDFs cần tối ưu)

## License

MIT License - Tự do sử dụng trong project của bạn

## Liên hệ

Nếu có vấn đề hoặc đề xuất, vui lòng tạo issue trên GitHub.
