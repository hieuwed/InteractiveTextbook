using System.Windows;
using System.Windows.Media.Animation;
using InteractiveTextbook.ViewModels;

namespace InteractiveTextbook.Views;

public partial class MainWindow : Window
{
    private PdfViewerViewModel? ViewModel => DataContext as PdfViewerViewModel;

    public MainWindow()
    {
        InitializeComponent();
        var vm = new PdfViewerViewModel();
        DataContext = vm;

        // Hook vào navigation commands để trigger animation
        Loaded += (s, e) =>
        {
            // Monitor IsAnimating property changes để trigger animation
            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(PdfViewerViewModel.IsAnimating) && vm.IsAnimating)
                {
                    // Trigger page flip animation
                    AnimatePageFlip();
                }
            };
        };
    }

    private void AnimatePageFlip()
    {
        if (LeftPageGrid == null || RightPageGrid == null) return;

        // Tạo storyboard animation với fade effect
        var storyboard = new Storyboard();

        // Fade out effect
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.3,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        // Fade in effect
        var fadeIn = new DoubleAnimation
        {
            From = 0.3,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(150),
            BeginTime = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        // Áp dụng fade animation
        Storyboard.SetTarget(fadeOut, LeftPageGrid);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(fadeOut);

        Storyboard.SetTarget(fadeIn, LeftPageGrid);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(fadeIn);

        // Cũng áp dụng cho right page
        var fadeOutRight = new DoubleAnimation
        {
            From = 1.0,
            To = 0.3,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        var fadeInRight = new DoubleAnimation
        {
            From = 0.3,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(150),
            BeginTime = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        Storyboard.SetTarget(fadeOutRight, RightPageGrid);
        Storyboard.SetTargetProperty(fadeOutRight, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(fadeOutRight);

        Storyboard.SetTarget(fadeInRight, RightPageGrid);
        Storyboard.SetTargetProperty(fadeInRight, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(fadeInRight);

        storyboard.Begin();
    }
}
