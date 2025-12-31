using System.Windows;
using System.Windows.Media.Animation;
using InteractiveTextbook.Models;

namespace InteractiveTextbook.Animations;

/// <summary>
/// Engine tạo hiệu ứng lật trang mượt mà giống lật sách thực tế
/// Sử dụng physics-based animation với momentum và friction
/// </summary>
public class PageFlipAnimationEngine
{
    // Constants cho physics
    private const double FRICTION = 0.95;  // Hệ số ma sát (0-1)
    private const double MIN_VELOCITY = 0.1; // Vận tốc tối thiểu để tiếp tục lật
    private const double FLIP_COMPLETE_THRESHOLD = 0.95; // Ngưỡng hoàn thành lật

    private PageFlipState _currentState = new();
    private bool _isAnimating = false;
    private System.Diagnostics.Stopwatch _animationTimer = new();
    private double _lastFrameTime = 0;

    public event Action<PageFlipState>? StateChanged;
    public event Action? FlipCompleted;
    public event Action? FlipCancelled;

    /// <summary>
    /// Bắt đầu interaction lật trang từ chuột
    /// </summary>
    public void StartFlip(int currentPage, int totalPages, double mouseX, double pageWidth, bool flipFromRight)
    {
        _currentState = new PageFlipState
        {
            CurrentPage = currentPage,
            NextPage = flipFromRight ? currentPage + 1 : currentPage - 1,
            MouseDeltaX = 0,
            IsFlippingForward = flipFromRight,
            FlipVelocity = 0,
            Progress = 0,
            FlipFromRight = flipFromRight
        };

        _isAnimating = true;
        _animationTimer.Restart();
        _lastFrameTime = 0;
    }

    /// <summary>
    /// Update vị trí lật trang theo chuột
    /// </summary>
    public void UpdateFlipPosition(double mouseX, double pageWidth)
    {
        if (!_isAnimating) return;

        _currentState.MouseDeltaX = mouseX;
        
        // Tính progress (0-1) dựa trên vị trí chuột
        _currentState.Progress = Math.Clamp(Math.Abs(mouseX) / pageWidth, 0, 1);

        StateChanged?.Invoke(_currentState);
    }

    /// <summary>
    /// Kết thúc interaction lật trang (thả chuột)
    /// </summary>
    public void EndFlip(double velocity, double pageWidth)
    {
        if (!_isAnimating) return;

        _currentState.FlipVelocity = velocity;

        // Nếu progress > 50% hoặc vận tốc đủ lớn, hoàn thành lật
        if (_currentState.Progress > 0.5 || Math.Abs(velocity) > 0.5)
        {
            AnimateFlipCompletion();
        }
        else
        {
            AnimateFlipCancellation();
        }
    }

    /// <summary>
    /// Animation hoàn thành lật trang (physics-based)
    /// </summary>
    private void AnimateFlipCompletion()
    {
        _animationTimer.Restart();
        _lastFrameTime = 0;
        
        // Chạy render loop
        System.Windows.Threading.DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(16) // 60 FPS
        };

        timer.Tick += (s, e) =>
        {
            double elapsed = _animationTimer.ElapsedMilliseconds / 1000.0;
            double deltaTime = elapsed - _lastFrameTime;
            _lastFrameTime = elapsed;

            // Áp dụng physics-based animation
            // Tăng tốc độ dựa trên momentum ban đầu
            _currentState.FlipVelocity *= FRICTION;
            _currentState.Progress += _currentState.FlipVelocity * deltaTime * 2.0;

            // Clamp progress to 0-1
            _currentState.Progress = Math.Clamp(_currentState.Progress, 0, 1);

            StateChanged?.Invoke(_currentState);

            // Kiểm tra hoàn thành
            if (_currentState.Progress >= FLIP_COMPLETE_THRESHOLD || 
                Math.Abs(_currentState.FlipVelocity) < MIN_VELOCITY)
            {
                _currentState.Progress = 1.0;
                StateChanged?.Invoke(_currentState);
                
                timer.Stop();
                _isAnimating = false;
                FlipCompleted?.Invoke();
            }
        };

        timer.Start();
    }

    /// <summary>
    /// Animation hủy lật trang (quay lại vị trí ban đầu)
    /// </summary>
    private void AnimateFlipCancellation()
    {
        _animationTimer.Restart();
        _lastFrameTime = 0;

        System.Windows.Threading.DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        timer.Tick += (s, e) =>
        {
            double elapsed = _animationTimer.ElapsedMilliseconds / 1000.0;
            double deltaTime = elapsed - _lastFrameTime;
            _lastFrameTime = elapsed;

            // Giảm progress mượt mà về 0
            _currentState.Progress -= deltaTime * 3.0; // Quay lại nhanh
            _currentState.Progress = Math.Max(0, _currentState.Progress);

            StateChanged?.Invoke(_currentState);

            if (_currentState.Progress <= 0)
            {
                _currentState.Progress = 0;
                StateChanged?.Invoke(_currentState);
                
                timer.Stop();
                _isAnimating = false;
                FlipCancelled?.Invoke();
            }
        };

        timer.Start();
    }

    /// <summary>
    /// Auto-flip animation (khi nhấn nút lật)
    /// </summary>
    public void AnimateAutoFlip(int currentPage, int totalPages, bool flipForward)
    {
        _currentState = new PageFlipState
        {
            CurrentPage = currentPage,
            NextPage = flipForward ? currentPage + 1 : currentPage - 1,
            IsFlippingForward = flipForward,
            Progress = 0,
            FlipVelocity = 1.5, // Vận tốc khởi đầu cao
            FlipFromRight = flipForward
        };

        _isAnimating = true;
        _animationTimer.Restart();
        _lastFrameTime = 0;

        System.Windows.Threading.DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        timer.Tick += (s, e) =>
        {
            double elapsed = _animationTimer.ElapsedMilliseconds / 1000.0;
            double deltaTime = elapsed - _lastFrameTime;
            _lastFrameTime = elapsed;

            _currentState.FlipVelocity *= FRICTION;
            _currentState.Progress += _currentState.FlipVelocity * deltaTime * 2.0;
            _currentState.Progress = Math.Clamp(_currentState.Progress, 0, 1);

            StateChanged?.Invoke(_currentState);

            if (_currentState.Progress >= FLIP_COMPLETE_THRESHOLD || 
                Math.Abs(_currentState.FlipVelocity) < MIN_VELOCITY)
            {
                _currentState.Progress = 1.0;
                StateChanged?.Invoke(_currentState);
                
                timer.Stop();
                _isAnimating = false;
                FlipCompleted?.Invoke();
            }
        };

        timer.Start();
    }

    public bool IsAnimating => _isAnimating;
    public PageFlipState CurrentState => _currentState;
}
