using System.Windows;
using System.Windows.Input;
using InteractiveTextbook.Models;
using InteractiveTextbook.ViewModels;
using InteractiveTextbook.Views;

namespace InteractiveTextbook.Views;

public partial class MainWindow : Window
{
    private PdfViewerViewModel? ViewModel => DataContext as PdfViewerViewModel;

    public MainWindow()
    {
        InitializeComponent();
        var vm = new PdfViewerViewModel();
        DataContext = vm;
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (FlipControlRight != null && vm != null && !FlipControlRight._animationEngine.IsAnimating && 
            vm.CurrentDocument != null && vm.CurrentPage + 2 <= vm.CurrentDocument.PageCount)
        {
            // Reset both controls to clean state
            if (FlipControlLeft != null)
            {
                FlipControlLeft.FlipProgress = 0;
                FlipControlLeft.Visibility = Visibility.Hidden;
            }
            FlipControlRight.FlipProgress = 0;
            
            // Show right control and animate forward
            FlipControlRight.Visibility = Visibility.Visible;
            FlipControlRight.AnimatePageFlip(true);  // true = flip forward
        }
    }

    private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (FlipControlLeft != null && vm != null && !FlipControlLeft._animationEngine.IsAnimating && vm.CurrentPage - 2 >= 1)
        {
            // Reset both controls to clean state
            FlipControlLeft.FlipProgress = 0;
            if (FlipControlRight != null)
            {
                FlipControlRight.FlipProgress = 0;
                FlipControlRight.Visibility = Visibility.Hidden;
            }
            
            // Show left control and animate backward
            FlipControlLeft.Visibility = Visibility.Visible;
            FlipControlLeft.AnimatePageFlip(false); // false = flip backward
        }
    }

    private void PageFlipControl_FlipCompleted(object? sender, PageFlipEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null || vm.CurrentDocument == null) return;

        var control = sender as PageFlipControl;
        if (control != null)
        {
            // Get the direction from the current animation state
            PageFlipState state = control._animationEngine.CurrentState;
            bool isFlipForward = state.IsFlippingForward;
            
            // Update page based on flip direction
            if (isFlipForward)
            {
                if (vm.CurrentPage + 2 <= vm.CurrentDocument.PageCount)
                {
                    vm.CurrentPage = vm.CurrentPage + 2;
                }
            }
            else
            {
                if (vm.CurrentPage - 2 >= 1)
                {
                    vm.CurrentPage = vm.CurrentPage - 2;
                }
            }
            
            // Load pages asynchronously
            vm.LoadPageAsync(vm.CurrentPage).ContinueWith(_ =>
            {
                // Reset flip control for next flip
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    control.FlipProgress = 0;
                    control.Visibility = Visibility.Hidden;
                    vm.IsAnimating = false;
                });
            });
        }
        else
        {
            vm.IsAnimating = false;
        }
    }

    private void PageFlipControl_FlipCancelled(object? sender, EventArgs e)
    {
        var vm = ViewModel;
        if (vm != null)
        {
            vm.IsAnimating = false;
            if (FlipControlLeft != null)
            {
                FlipControlLeft.FlipProgress = 0;
                FlipControlLeft.Visibility = Visibility.Hidden;
            }
            if (FlipControlRight != null)
            {
                FlipControlRight.FlipProgress = 0;
                FlipControlRight.Visibility = Visibility.Hidden;
            }
        }
    }
}

/// <summary>
/// Relay command implementation
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}
