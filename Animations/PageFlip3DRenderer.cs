using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using InteractiveTextbook.Models;

namespace InteractiveTextbook.Animations;

/// <summary>
/// Renderer 3D cho hiệu ứng lật trang với perspective thực tế
/// Tạo bóng, độ sâu, uốn cong và transformation 3D để giống lật sách thực tế
/// </summary>
public class PageFlip3DRenderer
{
    private const double DEPTH_FACTOR = 2000.0;
    private const double PERSPECTIVE_ANGLE = 65.0;

    /// <summary>
    /// Tính toán perspective transform cho trang lật với 3D effect thực tế
    /// </summary>
    public static Transform3D CalculateFlipTransform(PageFlipState state, double pageWidth, double pageHeight)
    {
        var transform = new Transform3DGroup();

        // Góc xoay dựa trên progress (0 - 180 độ)
        double rotationAngle = state.Progress * 180.0;

        // Xoay quanh cạnh phải hay cạnh trái
        double rotationCenterX = state.FlipFromRight ? pageWidth : 0;

        // 1. Translate để chuẩn bị rotation
        var translation = new TranslateTransform3D(-rotationCenterX, -pageHeight / 2.0, 0);

        // 2. Rotation quanh trục Y (giống lật sách)
        var rotation = new RotateTransform3D
        {
            Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), rotationAngle)
        };

        // 3. Translate lại vị trí cũ
        var translationBack = new TranslateTransform3D(rotationCenterX, pageHeight / 2.0, 0);

        // 4. Perspective projection
        var matrix = new Matrix3D();
        matrix.SetIdentity();
        
        // Áp dụng perspective depth
        matrix.M34 = -1.0 / DEPTH_FACTOR;
        var perspective = new MatrixTransform3D(matrix);

        // Thêm tất cả transform
        transform.Children.Add(perspective);
        transform.Children.Add(translation);
        transform.Children.Add(rotation);
        transform.Children.Add(translationBack);

        return transform;
    }

    /// <summary>
    /// Tính toán skew transform giả lập uốn cong trang
    /// </summary>
    public static Transform CalculatePageCurvatureTransform(PageFlipState state, double pageWidth, double pageHeight, bool isFlipPage)
    {
        if (!isFlipPage || state.Progress <= 0.01)
        {
            return Transform.Identity;
        }

        // Tạo skew effect để giả lập uốn cong
        var skewTransform = new SkewTransform();
        
        if (state.FlipFromRight)
        {
            // Lật từ phải: cạnh trái bị kéo xuống
            skewTransform.AngleX = -state.Progress * 5.0;
            skewTransform.CenterX = pageWidth;
        }
        else
        {
            // Lật từ trái: cạnh phải bị kéo xuống
            skewTransform.AngleX = state.Progress * 5.0;
            skewTransform.CenterX = 0;
        }

        return skewTransform;
    }

    /// <summary>
    /// Tính toán scale transform để giả lập perspective
    /// </summary>
    public static ScaleTransform CalculatePerspectiveScale(PageFlipState state, bool isFlipPage)
    {
        if (!isFlipPage)
        {
            return new ScaleTransform(1.0, 1.0);
        }

        // Trang đang lật sẽ bị thu nhỏ khi quay đi
        double scale = 1.0 - (state.Progress * 0.15);
        
        return new ScaleTransform(scale, 1.0);
    }

    /// <summary>
    /// Tính toán opacity của bóng dựa trên progress
    /// </summary>
    public static double CalculateShadowOpacity(PageFlipState state)
    {
        // Bóng tăng dần khi trang quay, đạt max ở progress = 0.5
        double shadowIntensity = Math.Sin(state.Progress * Math.PI);
        return Math.Clamp(shadowIntensity * 0.7, 0, 0.8);
    }

    /// <summary>
    /// Tính toán brush gradient cho hiệu ứng ánh sáng 3D thực tế
    /// </summary>
    public static Brush CreateFlipGradientBrush(PageFlipState state, double pageWidth)
    {
        // Ánh sáng di chuyển theo progress
        double lightStart = (1.0 - state.Progress);
        double lightEnd = Math.Max(0, lightStart - 0.3);

        var gradientStops = new GradientStopCollection
        {
            // Cạnh sáng (gần ánh sáng)
            new GradientStop(Color.FromArgb(255, 255, 255, 255), 0.0),
            new GradientStop(Color.FromArgb(220, 245, 245, 245), lightStart * 0.3),
            
            // Phần giữa (gradient chuyển tiếp)
            new GradientStop(Color.FromArgb(180, 220, 220, 220), lightStart * 0.6),
            new GradientStop(Color.FromArgb(150, 200, 200, 200), lightStart),
            
            // Phần tối (mặt sau)
            new GradientStop(Color.FromArgb(120, 180, 180, 180), lightEnd),
            new GradientStop(Color.FromArgb(80, 140, 140, 140), 1.0)
        };

        return new LinearGradientBrush(
            gradientStops,
            new Point(0, 0),
            new Point(1, 0)
        )
        {
            SpreadMethod = GradientSpreadMethod.Pad
        };
    }

    /// <summary>
    /// Tạo clip geometry cho trang đang lật (chỉ hiển thị phần chưa lật)
    /// </summary>
    public static Geometry CreateFlipClipGeometry(PageFlipState state, double pageWidth, double pageHeight)
    {
        if (state.Progress <= 0.01)
        {
            return new RectangleGeometry(new Rect(0, 0, pageWidth, pageHeight));
        }

        double clipWidth = pageWidth * (1.0 - state.Progress);

        if (state.FlipFromRight)
        {
            return new RectangleGeometry(new Rect(0, 0, clipWidth, pageHeight));
        }
        else
        {
            return new RectangleGeometry(new Rect(pageWidth - clipWidth, 0, clipWidth, pageHeight));
        }
    }

    /// <summary>
    /// Tạo drop shadow effect dựa trên trạng thái lật
    /// </summary>
    public static (double OffsetX, double OffsetY, double BlurRadius, double Opacity) CalculateShadowProperties(PageFlipState state, double pageWidth)
    {
        double progress = state.Progress;
        
        // Bóng di chuyển theo progress
        double shadowOffset = progress * pageWidth * 0.08;
        
        // Bóng tăng dần rồi giảm
        double shadowOpacity = CalculateShadowOpacity(state);
        
        // Blur tăng khi trang quay
        double blurRadius = Math.Sin(progress * Math.PI) * 15.0;

        return (shadowOffset, 8.0, blurRadius, shadowOpacity);
    }

    /// <summary>
    /// Tính toán độ sâu Z (giả lập trang bay ra khỏi mặt phẳng)
    /// </summary>
    public static double CalculateZDepth(PageFlipState state, double pageWidth)
    {
        // Trang bay ra khỏi mặt phẳng khi lật
        return Math.Sin(state.Progress * Math.PI) * (pageWidth * 0.2);
    }

    /// <summary>
    /// Tạo gradient cho mặt sau trang (chỉ hiển thị khi lật > 50%)
    /// </summary>
    public static Brush CreateBackSideGradient(PageFlipState state)
    {
        if (state.Progress < 0.5)
        {
            return new SolidColorBrush(Colors.Transparent);
        }

        var gradientStops = new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(255, 200, 200, 200), 0.0),
            new GradientStop(Color.FromArgb(255, 180, 180, 180), 1.0)
        };

        return new LinearGradientBrush(gradientStops);
    }
}
