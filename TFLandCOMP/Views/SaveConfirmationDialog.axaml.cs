using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TFLandCOMP.Views
{
    public enum SaveConfirmationResult
    {
        Yes,
        No,
        Cancel
    }

    public partial class SaveConfirmationDialog : Window
    {
        public SaveConfirmationResult? Result { get; private set; }

        public SaveConfirmationDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            Result = SaveConfirmationResult.Yes;
            Close();
        }

        private void OnDontSave(object sender, RoutedEventArgs e)
        {
            Result = SaveConfirmationResult.No;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Result = SaveConfirmationResult.Cancel;
            Close();
        }
    }
}
