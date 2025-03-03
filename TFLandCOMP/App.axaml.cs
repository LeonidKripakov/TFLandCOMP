using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TFLandCOMP.ViewModels;
using TFLandCOMP.Views;


namespace TFLandCOMP
{
    public class App : Application
    {
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Создаем единственный экземпляр DatabaseService



            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}
