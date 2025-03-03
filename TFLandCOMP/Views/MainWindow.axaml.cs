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

        // ���������� ��� ������ "����������"
        private async void OnCopy(object sender, RoutedEventArgs e)
        {
            // �������� ������� fileTextBox �� ����������� ������
            var fileTextBox = this.FindControl<TextBox>("fileTextBox");
            if (fileTextBox != null)
            {
                int start = fileTextBox.SelectionStart;
                int end = fileTextBox.SelectionEnd;
                // ���� ���� ���������, �������� ���, ����� �������� ���� �����
                string textToCopy = (start < end)
                    ? fileTextBox.Text.Substring(start, end - start)
                    : fileTextBox.Text;

                if (!string.IsNullOrEmpty(textToCopy))
                {
                    await this.Clipboard.SetTextAsync(textToCopy);
                }
            }
        }

        // ���������� ��� ������ "��������"
        private async void OnCut(object sender, RoutedEventArgs e)
        {
            var fileTextBox = this.FindControl<TextBox>("fileTextBox");
            if (fileTextBox != null)
            {
                int start = fileTextBox.SelectionStart;
                int end = fileTextBox.SelectionEnd;
                if (start < end)
                {
                    // �������� ���������� �����
                    string selectedText = fileTextBox.Text.Substring(start, end - start);
                    // �������� ���������� ����� � ����� ������
                    await this.Clipboard.SetTextAsync(selectedText);
                    // ������� ���������� ����� � ������� �������� SelectedText
                    fileTextBox.SelectedText = "";
                }
            }
        }

        // ���������� ��� ������ "��������"
        // ������� ������������ � fileTextBox �� ������� �������
        private async void OnPaste(object sender, RoutedEventArgs e)
        {
            var fileTextBox = this.FindControl<TextBox>("fileTextBox");
            if (fileTextBox != null)
            {
                var clipboardText = await this.Clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    int caretIndex = fileTextBox.CaretIndex;
                    // ��������� ����� �� ������ ������ � ������� �������
                    fileTextBox.Text = fileTextBox.Text.Insert(caretIndex, clipboardText);
                    // ��������� ������� �������
                    fileTextBox.CaretIndex = caretIndex + clipboardText.Length;
                }
            }
        }
    }
}
