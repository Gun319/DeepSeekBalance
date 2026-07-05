using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using DeepSeekBalance.ViewModels;

namespace DeepSeekBalance.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == DataContextProperty && DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += (_, a) =>
                {
                    if (a.PropertyName == nameof(MainWindowViewModel.IsDark))
                        ApplyTheme(vm.IsDark);
                };
                ApplyTheme(vm.IsDark);
            }
        };

        Closing += (_, _) => (DataContext as MainWindowViewModel)?.DisposeTimer();
    }

    private static void ApplyTheme(bool dark)
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
