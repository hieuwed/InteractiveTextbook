using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using InteractiveTextbook.Animations;
using InteractiveTextbook.Models;

namespace InteractiveTextbook.Views;

public partial class PageFlipControl : UserControl
{
    public readonly PageFlipAnimationEngine _animationEngine = new();
    private Point _mouseDownPoint = new();
    private double _previousMouseX = 0;
    private double _lastMouseVelocity = 0;
    private bool _isMouseDown = false;
    private double _pageWidth => this.ActualWidth > 0 ? this.ActualWidth : 500;
    private double _pageHeight => this.ActualHeight > 0 ? this.ActualHeight : 700;

    public static readonly DependencyProperty CurrentPageImageProperty = DependencyProperty.Register(
        nameof(CurrentPageImage),
        typeof(BitmapSource),
        typeof(PageFlipControl),
        new PropertyMetadata(null, OnCurrentPageImageChanged));

    public static readonly DependencyProperty NextPageImageProperty = DependencyProperty.Register(
        nameof(NextPageImage),
        typeof(BitmapSource),
        typeof(PageFlipControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty FlipProgressProperty = DependencyProperty.Register(
        nameof(FlipProgress),
        typeof(double),
        typeof(PageFlipControl),
        new PropertyMetadata(0.0, OnFlipProgressChanged));

    public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(
        nameof(CurrentPage),
        typeof(int),
        typeof(PageFlipControl),
        new PropertyMetadata(1));

    public static readonly DependencyProperty TotalPagesProperty = DependencyProperty.Register(
        nameof(TotalPages),
        typeof(int),
        typeof(PageFlipControl),
        new PropertyMetadata(0));

    public event EventHandler<PageFlipEventArgs>? FlipCompleted;
    public event EventHandler? FlipCancelled;

    public PageFlipControl()
    {
        InitializeComponent();
        
        _animationEngine.StateChanged += OnAnimationStateChanged;
        _animationEngine.FlipCompleted += OnFlipCompleted;
        _animationEngine.FlipCancelled += OnFlipCancelled;

        this.MouseDown += PageFlipControl_MouseDown;
        this.MouseMove += PageFlipControl_MouseMove;
        this.MouseUp += PageFlipControl_MouseUp;
        this.MouseLeave += PageFlipControl_MouseLeave;
    }

    public BitmapSource? CurrentPageImage
    {
        get => (BitmapSource)GetValue(CurrentPageImageProperty);
        set => SetValue(CurrentPageImageProperty, value);
    }

    public BitmapSource? NextPageImage
    {
        get => (BitmapSource)GetValue(NextPageImageProperty);
        set => SetValue(NextPageImageProperty, value);
    }

    public double FlipProgress
    {
        get => (double)GetValue(FlipProgressProperty);
        set => SetValue(FlipProgressProperty, value);
    }

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    private static void OnCurrentPageImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageFlipControl control && e.NewValue is BitmapSource bitmap)
        {
            control.FlipPageImage.Source = bitmap;
        }
    }

    private static void OnFlipProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageFlipControl control)
        {
            control.UpdateFlipVisualization();
        }
    }

    private void PageFlipControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_animationEngine.IsAnimating) return;

        _mouseDownPoint = e.GetPosition(this);
        _isMouseDown = true;
        _previousMouseX = _mouseDownPoint.X;
        _lastMouseVelocity = 0;

        // Determine flip direction based on mouse position
        // If mouse is on right side, flip forward (right to left)
        bool flipForward = _mouseDownPoint.X > _pageWidth / 2;

        _animationEngine.StartFlip(CurrentPage, TotalPages, _mouseDownPoint.X, _pageWidth, flipForward);
        e.Handled = true;
    }

    private void PageFlipControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isMouseDown) return;

        Point currentPoint = e.GetPosition(this);
        double deltaX = currentPoint.X - _mouseDownPoint.X;
        
        // Tính vận tốc
        _lastMouseVelocity = (currentPoint.X - _previousMouseX) / 16.0; // 60 FPS
        _previousMouseX = currentPoint.X;

        _animationEngine.UpdateFlipPosition(deltaX, _pageWidth);
    }

    private void PageFlipControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isMouseDown) return;

        _isMouseDown = false;
        _animationEngine.EndFlip(_lastMouseVelocity, _pageWidth);
        e.Handled = true;
    }

    private void PageFlipControl_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isMouseDown)
        {
            _isMouseDown = false;
            _animationEngine.EndFlip(_lastMouseVelocity, _pageWidth);
        }
    }

    private void OnAnimationStateChanged(PageFlipState state)
    {
        FlipProgress = state.Progress;
        UpdateFlipVisualization();
    }

    private void UpdateFlipVisualization()
    {
        double progress = FlipProgress;

        // 1. Cập nhật transform rotation 2D cho flip page (giả lập 3D)
        var transformGroup = new TransformGroup();
        
        // Perspective skew để giả lập 3D rotation
        var skewTransform = new SkewTransform();
        if (progress > 0)
        {
            // Tạo hiệu ứng uốn cong theo hướng lật
            skewTransform.AngleX = -progress * 8.0;
            skewTransform.CenterX = _pageWidth / 2;
        }
        transformGroup.Children.Add(skewTransform);

        FlipPageBorder.RenderTransform = transformGroup;

        // 2. Cập nhật gradient ánh sáng 
        var gradientBrush = PageFlip3DRenderer.CreateFlipGradientBrush(
            new PageFlipState { Progress = progress },
            _pageWidth
        );
        LightGradient.Fill = gradientBrush;
        LightGradient.Opacity = 0.4 * Math.Sin(progress * Math.PI);

        // 3. Cập nhật shadow
        var (offsetX, offsetY, blurRadius, shadowOpacity) = PageFlip3DRenderer.CalculateShadowProperties(
            new PageFlipState { Progress = progress },
            _pageWidth
        );

        try
        {
            var shadowEffect = new System.Windows.Media.Effects.BlurEffect
            {
                Radius = blurRadius
            };
            ShadowBorder.Effect = shadowEffect;
            ShadowBorder.Opacity = shadowOpacity;
        }
        catch { }

        // 4. Cập nhật back side (chỉ hiển thị khi progress > 50%)
        FlipPageBackSideBorder.Opacity = progress > 0.5 ? 1.0 - (1.0 - progress) * 2.0 : 0;

        // 5. Cập nhật depth effect
        DepthOverlay.Opacity = progress * 0.3;

        // 6. Clip geometry cho flip page (chỉ hiển thị phần chưa lật)
        var clipGeometry = PageFlip3DRenderer.CreateFlipClipGeometry(
            new PageFlipState { Progress = progress },
            _pageWidth,
            _pageHeight
        );
        FlipPageGrid.Clip = clipGeometry;
    }

    public void AnimatePageFlip(bool flipForward)
    {
        // Ensure control is measured with new size before animation
        this.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        this.Arrange(new System.Windows.Rect(this.DesiredSize));
        
        // Trigger auto-flip animation
        _animationEngine.AnimateAutoFlip(CurrentPage, TotalPages, flipForward, 1.8);
    }

    private void OnFlipCompleted()
    {
        // Fire event BEFORE reset to preserve state
        FlipCompleted?.Invoke(this, new PageFlipEventArgs { CurrentPage = CurrentPage });
        
        // Reset AFTER event has been processed
        _animationEngine.Reset();
    }

    private void OnFlipCancelled()
    {
        // Fire event BEFORE reset
        FlipCancelled?.Invoke(this, EventArgs.Empty);
        
        // Reset AFTER event
        _animationEngine.Reset();
    }
}

public class PageFlipEventArgs : EventArgs
{
    public int CurrentPage { get; set; }
}
