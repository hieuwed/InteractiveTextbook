using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteractiveTextbook.Extensions;
using InteractiveTextbook.Models;
using InteractiveTextbook.Services;

namespace InteractiveTextbook.ViewModels;

public partial class PdfViewerViewModel : ObservableObject
{
    private readonly PdfRenderService _pdfService = new();

    [ObservableProperty]
    private PdfDocument? currentDocument;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private double zoomLevel = 1.0;

    [ObservableProperty]
    private string statusMessage = "Sẵn sàng";

    [ObservableProperty]
    private bool documentLoaded = false;

    [ObservableProperty]
    private System.Windows.Media.Imaging.BitmapSource? leftPageImage;

    [ObservableProperty]
    private System.Windows.Media.Imaging.BitmapSource? rightPageImage;

    [ObservableProperty]
    private bool isAnimating = false;

    private ObservableCollection<BitmapSourceWrapper> _displayedPages = new();
    
    public ObservableCollection<BitmapSourceWrapper> DisplayedPages
    {
        get => _displayedPages;
        set
        {
            if (_displayedPages != value)
            {
                _displayedPages = value;
                OnPropertyChanged(nameof(DisplayedPages));
            }
        }
    }

    public PdfViewerViewModel()
    {
        // Constructor
    }

    [RelayCommand]
    public void LoadPdf()
    {
        LoadPdfAsync().FireAndForget();
    }

    public async Task LoadPdfAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() != true)
                return;

            StatusMessage = $"Đang tải {Path.GetFileName(dialog.FileName)}...";

            // Clear old document data IMMEDIATELY on UI thread
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                LeftPageImage = null;
                RightPageImage = null;
                DisplayedPages.Clear();
                CurrentDocument = null;
                CurrentPage = 1;
                DocumentLoaded = false;
                IsAnimating = false;
            });

            // Wait a bit to ensure UI is cleared
            await Task.Delay(100);

            // Load new document
            CurrentDocument = await _pdfService.LoadPdfAsync(dialog.FileName);

            if (CurrentDocument == null)
            {
                StatusMessage = "Lỗi: Không thể tải tài liệu PDF";
                return;
            }

            CurrentPage = 1;
            DocumentLoaded = true;
            await LoadPageAsync(1);
            await _pdfService.CacheAdjacentPagesAsync(1);

            StatusMessage = $"Đã tải: {CurrentDocument.FileName} ({CurrentDocument.PageCount} trang)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                LeftPageImage = null;
                RightPageImage = null;
                DisplayedPages.Clear();
                DocumentLoaded = false;
                IsAnimating = false;
            });
        }
    }

    [RelayCommand]
    public void NextPage()
    {
        NextPageAsync().FireAndForget();
    }

    public async Task NextPageAsync()
    {
        if (CurrentDocument == null || IsAnimating) return;
        
        // Nếu trang hiện tại + 1 >= tổng trang thì không thể lật tiếp
        if (CurrentPage + 2 > CurrentDocument.PageCount) return;

        IsAnimating = true;
        // Animation sẽ được handle bởi PageFlipControl
        // Chỉ cần delay để simulate animation
        await Task.Delay(400);
        
        if (!IsAnimating) return; // Check if still animating (not cancelled)
        
        CurrentPage = CurrentPage + 2;
        await LoadPageAsync(CurrentPage);
        IsAnimating = false;
    }

    [RelayCommand]
    public void PreviousPage()
    {
        PreviousPageAsync().FireAndForget();
    }

    public async Task PreviousPageAsync()
    {
        if (CurrentDocument == null || IsAnimating) return;
        
        // Nếu trang hiện tại - 2 < 1 thì không thể lật lại
        if (CurrentPage - 2 < 1) return;

        IsAnimating = true;
        // Animation sẽ được handle bởi PageFlipControl
        await Task.Delay(400);
        
        if (!IsAnimating) return; // Check if still animating (not cancelled)
        
        CurrentPage = CurrentPage - 2;
        await LoadPageAsync(CurrentPage);
        IsAnimating = false;
    }

    [RelayCommand]
    public void ZoomIn()
    {
        var newZoom = Math.Min(ZoomLevel + 0.1, 3.0);
        ZoomLevel = newZoom;
    }

    [RelayCommand]
    public void ZoomOut()
    {
        var newZoom = Math.Max(ZoomLevel - 0.1, 0.5);
        ZoomLevel = newZoom;
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    public async Task LoadPageAsync(int pageNumber)
    {
        try
        {
            if (CurrentDocument == null) return;

            // Load left page (current page)
            var leftBitmap = await _pdfService.RenderPageAsync(pageNumber, ZoomLevel);
            if (leftBitmap != null)
            {
                LeftPageImage = leftBitmap;
            }

            // Load right page (next page)
            if (pageNumber + 1 <= CurrentDocument.PageCount)
            {
                var rightBitmap = await _pdfService.RenderPageAsync(pageNumber + 1, ZoomLevel);
                if (rightBitmap != null)
                {
                    RightPageImage = rightBitmap;
                }
            }
            else
            {
                RightPageImage = null;
            }

            // Keep DisplayedPages updated for compatibility
            DisplayedPages.Clear();
            if (leftBitmap != null)
            {
                DisplayedPages.Add(new BitmapSourceWrapper { Source = leftBitmap });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi render trang: {ex.Message}";
        }
    }

    partial void OnZoomLevelChanged(double value)
    {
        if (DocumentLoaded)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(async () =>
            {
                await LoadPageAsync(CurrentPage);
            });
        }
    }
}

/// <summary>
/// Wrapper để bind BitmapSource trong XAML
/// </summary>
public class BitmapSourceWrapper
{
    public System.Windows.Media.Imaging.BitmapSource? Source { get; set; }
}
