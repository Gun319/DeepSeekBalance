using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DeepSeekBalance.ViewModels;
using DeepSeekBalance.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace DeepSeekBalance;

public partial class App : Application
{
    public App()
    {
        Services = ConfigureServices();
    }

    public new static App Current => (App)Application.Current!;

    public IServiceProvider Services { get; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<Services.IConfigService, Services.ConfigService>();
        services.AddHttpClient<Services.IDeepSeekApiService, Services.DeepSeekApiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.deepseek.com");
        });
        services.AddHttpClient<Services.IPlatformApiService, Services.PlatformApiService>();
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
