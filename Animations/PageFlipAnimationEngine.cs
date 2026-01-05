using System.Windows;
using System.Windows.Media.Animation;
using InteractiveTextbook.Models;

namespace InteractiveTextbook.Animations;

/// <summary>
/// Engine tạo hiệu ứng lật trang mượt mà giống lật sách thực tế
/// Sử dụng physics-based animation với momentum, friction và easing function
/// </summary>
public class PageFlipAnimationEngine
{
    // Physics constants
    private const double FRICTION = 0.92;           // Hệ số ma sát (0-1), càng cao càng mượt
    private const double MIN_VELOCITY = 0.05;       // Vận tốc tối thiểu để tiếp tục lật
    private const double FLIP_COMPLETE_THRESHOLD = 0.98; // Ngưỡng hoàn thành lật

    private PageFlipState _currentState = new();
    private bool _isAnimating = false;
    private System.Diagnostics.Stopwatch _animationTimer = new();
    private double _lastFrameTime = 0;
    private System.Windows.Threading.DispatcherTimer? _activeTimer;

    public event Action<PageFlipState>? StateChanged;
    public event Action? FlipCompleted;
    public event Action? FlipCancelled;

    /// <summary>
    /// Bắt đầu interaction lật trang từ chuột/touch
    /// </summary>
    public void StartFlip(int currentPage, int totalPages, double mouseX, double pageWidth, bool flipFromRight)
    {
        // Dừng animation hiện tại nếu có
        _activeTimer?.Stop();

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

        StateChanged?.Invoke(_currentState);
    }

    /// <summary>
    /// Update vị trí lật trang theo chuột/touch
    /// </summary>
    public void UpdateFlipPosition(double mouseX, double pageWidth)
    {
        if (!_isAnimating) return;

        _currentState.MouseDeltaX = mouseX;
        
        // Tính progress (0-1) dựa trên vị trí chuột
        // Thêm easing để không quá nhạy ở đầu
        double rawProgress = Math.Abs(mouseX) / pageWidth;
        _currentState.Progress = Math.Clamp(EaseOutQuad(rawProgress), 0, 1);

        // Tính vận tốc dựa trên delta
        double elapsedMs = _animationTimer.ElapsedMilliseconds;
        if (elapsedMs > 0)
        {
            _currentState.FlipVelocity = mouseX / (pageWidth * (elapsedMs / 1000.0));
        }

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
        if (_currentState.Progress > 0.5 || Math.Abs(velocity) > 500)
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
        
        _activeTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };

        _activeTimer.Tick += (s, e) =>
        {
            if (_activeTimer == null) return; // Safety check
            
            double elapsed = _animationTimer.ElapsedMilliseconds / 1000.0;
            double deltaTime = Math.Min(elapsed - _lastFrameTime, 0.03); // Cap deltaTime
            _lastFrameTime = elapsed;

            // Áp dụng physics-based animation
            // Giảm vận tốc do ma sát
            _currentState.FlipVelocity *= FRICTION;
            
            // Cập nhật progress dựa trên velocity
            // Tăng tốc độ animation (nhân 3) để chuyển động nhanh hơn
            _currentState.Progress += _currentState.FlipVelocity * deltaTime * 3.0;

            // Clamp progress to 0-1
            _currentState.Progress = Math.Clamp(_currentState.Progress, 0, 1);

            StateChanged?.Invoke(_currentState);

            // Kiểm tra hoàn thành
            if (_currentState.Progress >= FLIP_COMPLETE_THRESHOLD || 
                Math.Abs(_currentState.FlipVelocity) < MIN_VELOCITY)
            {
                _currentState.Progress = 1.0;
                StateChanged?.Invoke(_currentState);
                
                _activeTimer?.Stop();
                _activeTimer = null;
                _isAnimating = false;
                FlipCompleted?.Invoke();
            }
        };

        _activeTimer.Start();
    }

    /// <summary>
    /// Animation hủy lật trang (quay lại vị trí ban đầu)
    /// </summary>
    private void AnimateFlipCancellation()
    {
        _animationTimer.Restart();
        _lastFrameTime = 0;

        _activeTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _activeTimer.Tick += (s, e) =>
        {
            double elapsed = _animationTimer.ElapsedMilliseconds / 1000.0;
            double deltaTime = Math.Min(elapsed - _lastFrameTime, 0.03);
            _lastFrameTime = elapsed;

            // Giảm progress mượt mà về 0 với easing
            _currentState.Progress -= deltaTime * 4.0; // Quay lại nhanh
            _currentState.Progress = Math.Max(0, _currentState.Progress);

            StateChanged?.Invoke(_currentState);

            if (_currentState.Progress <= 0)
            {
                _currentState.Progress = 0;
                StateChanged?.Invoke(_currentState);
                
                _activeTimer?.Stop();
                _isAnimating = false;
                FlipCancelled?.Invoke();
            }
        };

        _activeTimer.Start();
    }

    /// <summary>
    /// Auto-flip animation (khi nhấn nút lật hoặc swipe nhanh)
    /// </summary>
    public void AnimateAutoFlip(int currentPage, int totalPages, bool flipForward, double initialVelocity = 1.5)
    {
        _activeTimer?.Stop();

        _currentState = new PageFlipState
        {
            CurrentPage = currentPage,
            NextPage = flipForward ? currentPage + 1 : currentPage - 1,
            IsFlippingForward = flipForward,
            Progress = 0,
            FlipVelocity = initialVelocity, // Vận tốc khởi đầu cao
            FlipFromRight = flipForward
        };

        _isAnimating = true;
        _animationTimer.Restart();
        _lastFrameTime = 0;

        _activeTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _activeTimer.Tick += (s, e) =>
        {
            double elapsed = _animationTimer.ElapsedMilliseconds / 1000.0;
            double deltaTime = Math.Min(elapsed - _lastFrameTime, 0.03);
            _lastFrameTime = elapsed;

            _currentState.FlipVelocity *= FRICTION;
            _currentState.Progress += _currentState.FlipVelocity * deltaTime * 3.0;
            _currentState.Progress = Math.Clamp(_currentState.Progress, 0, 1);

            StateChanged?.Invoke(_currentState);

            if (_currentState.Progress >= FLIP_COMPLETE_THRESHOLD || 
                Math.Abs(_currentState.FlipVelocity) < MIN_VELOCITY)
            {
                _currentState.Progress = 1.0;
                StateChanged?.Invoke(_currentState);
                
                _activeTimer?.Stop();
                _isAnimating = false;
                FlipCompleted?.Invoke();
            }
        };

        _activeTimer.Start();
    }

    /// <summary>
    /// Easing function: Ease Out Quad (bắt đầu nhanh, kết thúc chậm)
    /// </summary>
    private static double EaseOutQuad(double t)
    {
        return 1.0 - (1.0 - t) * (1.0 - t);
    }

    /// <summary>
    /// Easing function: Ease In Out Cubic (mượt mà từ đầu đến cuối)
    /// </summary>
    private static double EaseInOutCubic(double t)
    {
        return t < 0.5 
            ? 4.0 * t * t * t 
            : 1.0 - Math.Pow(-2.0 * t + 2.0, 3.0) / 2.0;
    }

    public bool IsAnimating => _isAnimating;
    public PageFlipState CurrentState => _currentState;

    /// <summary>
    /// Reset engine để sẵn sàng cho lần lật tiếp theo
    /// </summary>
    public void Reset()
    {
        if (_activeTimer != null)
        {
            _activeTimer.Stop();
            _activeTimer = null;
        }
        _isAnimating = false;
        _currentState = new PageFlipState { Progress = 0 };
        _lastFrameTime = 0;
    }
}
