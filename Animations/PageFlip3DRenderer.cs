using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using InteractiveTextbook.Models;

namespace InteractiveTextbook.Animations;

/// <summary>
/// Renderer 3D cho hiệu ứng lật trang
/// Tạo bóng, độ sâu, và transformation 3D để giống lật sách thực tế
/// </summary>
public class PageFlip3DRenderer
{
    /// <summary>
    /// Tính toán perspective transform cho trang lật
    /// </summary>
    public static Transform3D CalculateFlipTransform(PageFlipState state, double pageWidth, double pageHeight)
    {
        var transform = new Transform3DGroup();

        // Góc xoay dựa trên progress (0 - 180 độ)
        double rotationAngle = state.Progress * 180.0;

        // Xoay quanh cạnh phải hay cạnh trái
        double rotationCenterX = state.FlipFromRight ? pageWidth : 0;

        // Tạo rotation transform
        var rotation = new RotateTransform3D
        {
            Rotation = new AxisAngleRotation3D(
                new Vector3D(0, 1, 0),
                rotationAngle
            ),
            CenterX = rotationCenterX,
            CenterY = pageHeight / 2.0,
            CenterZ = 0
        };

        transform.Children.Add(rotation);

        // Thêm perspective transform để tạo hiệu ứng 3D
        var projection = new Matrix3D();
        double perspectiveDepth = 1000;
        projection.SetIdentity();
        
        // Apply perspective
        projection.M34 = -1.0 / perspectiveDepth;
        transform.Children.Add(new MatrixTransform3D(projection));

        return transform;
    }

    /// <summary>
    /// Tính toán opacity của bóng dựa trên progress
    /// </summary>
    public static double CalculateShadowOpacity(PageFlipState state)
    {
        // Bóng tăng dần khi trang quay
        // Max opacity ở progress = 0.5 (giữa cuộc lật)
        return Math.Sin(state.Progress * Math.PI) * 0.6;
    }

    /// <summary>
    /// Tính toán brush gradient cho hiệu ứng ánh sáng 3D
    /// </summary>
    public static Brush CreateFlipGradientBrush(PageFlipState state, double pageWidth)
    {
        // Gradient từ sáng (cạnh gần) đến tối (cạnh xa)
        double lightPosition = (1.0 - state.Progress) * pageWidth;

        var gradientStops = new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(255, 255, 255, 255), 0.0),
            new GradientStop(Color.FromArgb(200, 240, 240, 240), lightPosition / pageWidth * 0.5),
            new GradientStop(Color.FromArgb(150, 200, 200, 200), lightPosition / pageWidth),
            new GradientStop(Color.FromArgb(100, 160, 160, 160), 1.0)
        };

        return new LinearGradientBrush(
            gradientStops,
            new Point(0, 0),
            new Point(1, 0)
        );
    }

    /// <summary>
    /// Tạo clip geometry cho trang đang lật
    /// Để chỉ hiển thị phần trang chưa lật
    /// </summary>
    public static Geometry CreateFlipClipGeometry(PageFlipState state, double pageWidth, double pageHeight)
    {
        double clipWidth = pageWidth * (1.0 - state.Progress);

        // Clip từ phải nếu lật tiến, từ trái nếu lật lùi
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
    /// Tính toán bóng dưới trang (drop shadow)
    /// </summary>
    public static (double OffsetX, double OffsetY, double BlurRadius) CalculateShadowProperties(PageFlipState state, double pageWidth)
    {
        // Bóng di chuyển theo progress
        double shadowOffset = state.Progress * pageWidth * 0.1;
        double blurRadius = state.Progress * 10.0;

        return (shadowOffset, 5.0, blurRadius);
    }
}
