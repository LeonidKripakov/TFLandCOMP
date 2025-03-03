using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TFLandCOMP.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Обработчик для кнопки "Копировать"
        private async void OnCopy(object sender, RoutedEventArgs e)
        {
            // Получаем элемент fileTextBox из визуального дерева
            var fileTextBox = this.FindControl<TextBox>("fileTextBox");
            if (fileTextBox != null)
            {
                int start = fileTextBox.SelectionStart;
                int end = fileTextBox.SelectionEnd;
                // Если есть выделение, копируем его, иначе копируем весь текст
                string textToCopy = (start < end)
                    ? fileTextBox.Text.Substring(start, end - start)
                    : fileTextBox.Text;

                if (!string.IsNullOrEmpty(textToCopy))
                {
                    await this.Clipboard.SetTextAsync(textToCopy);
                }
            }
        }

        // Обработчик для кнопки "Вырезать"
        private async void OnCut(object sender, RoutedEventArgs e)
        {
            var fileTextBox = this.FindControl<TextBox>("fileTextBox");
            if (fileTextBox != null)
            {
                int start = fileTextBox.SelectionStart;
                int end = fileTextBox.SelectionEnd;
                if (start < end)
                {
                    // Получаем выделенный текст
                    string selectedText = fileTextBox.Text.Substring(start, end - start);
                    // Копируем выделенный текст в буфер обмена
                    await this.Clipboard.SetTextAsync(selectedText);
                    // Удаляем выделенный текст с помощью свойства SelectedText
                    fileTextBox.SelectedText = "";
                }
            }
        }

        // Обработчик для кнопки "Вставить"
        // Вставка производится в fileTextBox по позиции каретки
        private async void OnPaste(object sender, RoutedEventArgs e)
        {
            var fileTextBox = this.FindControl<TextBox>("fileTextBox");
            if (fileTextBox != null)
            {
                var clipboardText = await this.Clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    int caretIndex = fileTextBox.CaretIndex;
                    // Вставляем текст из буфера обмена в позицию каретки
                    fileTextBox.Text = fileTextBox.Text.Insert(caretIndex, clipboardText);
                    // Обновляем позицию каретки
                    fileTextBox.CaretIndex = caretIndex + clipboardText.Length;
                }
            }
        }
    }
}
