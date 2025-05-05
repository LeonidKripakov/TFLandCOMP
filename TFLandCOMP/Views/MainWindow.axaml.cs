using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaEdit;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Highlighting;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;

using TFLandCOMP.ViewModels;
using SkiaSharp;
using System.Net.NetworkInformation;
using System.Linq;

namespace TFLandCOMP.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string currentFilePath = null;

        private string _currentFileName = "Текст из файла:";
        public string CurrentFileName
        {
            get => _currentFileName;
            set
            {
                if (_currentFileName != value)
                {
                    _currentFileName = value;
                    OnPropertyChanged();
                }
            }
        }


        private void LoadRustSyntaxHighlighting(TextEditor editor)
        {
            //var assembly = Assembly.GetExecutingAssembly();

            //using (Stream s = assembly.GetManifestResourceStream("TFLandCOMP.Highlighting.Rust.xshd"))
            //{
            //    using var reader = XmlReader.Create(s);

            //    editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            //}

            
        }

        private Stack<(string text, int caretIndex)> undoStack = new Stack<(string text, int caretIndex)>();
        private Stack<(string text, int caretIndex)> redoStack = new Stack<(string text, int caretIndex)>();

        private bool isInternalUpdate = false;
        private bool isModified = false;
        private string lastText = "";

        private bool _forceClose = false;
        private enum LastActionType { None, Insert, Delete }
        private (LastActionType ActionType, string Text, int Position) lastAction = (LastActionType.None, "", 0);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewViewModel();
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                LoadRustSyntaxHighlighting(fileEditor); 
               
                fileEditor.PointerWheelChanged += FileEditor_PointerWheelChanged;
                fileEditor.TextChanged += (s, e) =>
                {
                    var newText = fileEditor.Text;
                    if (!isInternalUpdate)
                    {
                        if (lastText != newText)
                        {
                            undoStack.Push((lastText, fileEditor.CaretOffset));
                            redoStack.Clear();
                            isModified = true;

                            if (newText.Length > lastText.Length)
                            {
                                int diff = newText.Length - lastText.Length;
                                int diffIndex = 0;
                                while (diffIndex < lastText.Length && lastText[diffIndex] == newText[diffIndex])
                                    diffIndex++;
                                string inserted = newText.Substring(diffIndex, diff);
                                lastAction = (LastActionType.Insert, inserted, diffIndex);
                            }
                            else if (newText.Length < lastText.Length)
                            {
                                int diff = lastText.Length - newText.Length;
                                int diffIndex = 0;
                                while (diffIndex < newText.Length && lastText[diffIndex] == newText[diffIndex])
                                    diffIndex++;
                                string deleted = lastText.Substring(diffIndex, diff);
                                lastAction = (LastActionType.Delete, deleted, diffIndex);
                            }
                            else
                            {
                                lastAction = (LastActionType.None, "", 0);
                            }
                        }
                        lastText = newText;
                    }
                };

                fileEditor.PointerWheelChanged += FileEditor_PointerWheelChanged;
            }
        }

        private void FileEditor_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                var editor = sender as TextEditor;
                if (editor != null)
                {
                    double newSize = editor.FontSize + (e.Delta.Y * 0.1);
                    newSize = Math.Max(8, Math.Min(72, newSize));
                    editor.FontSize = newSize;
                    e.Handled = true;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Методы работы с файлами

        private void OnNewFile(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                SetTextInternal(fileEditor, "");
                undoStack.Clear();
                redoStack.Clear();
                currentFilePath = null;
                CurrentFileName = "Новый файл";
                isModified = false;
            }
        }

        private async void OnOpenFile(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                var openDialog = new OpenFileDialog
                {
                    Title = "Открыть файл",
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Текстовые файлы", Extensions = new List<string> { "txt" } }
                    }
                };
                var result = await openDialog.ShowAsync(this);
                if (result != null && result.Length > 0)
                {
                    string fileName = result[0];
                    if (File.Exists(fileName))
                    {
                        string content = await File.ReadAllTextAsync(fileName);
                        SetTextInternal(fileEditor, content);
                        undoStack.Clear();
                        redoStack.Clear();
                        currentFilePath = fileName;
                        CurrentFileName = Path.GetFileName(fileName);
                        isModified = false;
                    }
                }
            }
        }

        private async void OnSaveFile(object sender, RoutedEventArgs e)
        {
            await OnSaveFileAsync();
        }

        private async void OnSaveFileAS(object sender, RoutedEventArgs e)
        {
            await OnSaveAsAsync();
        }

        private async Task OnSaveFileAsync()
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    await File.WriteAllTextAsync(currentFilePath, fileEditor.Text ?? "");
                    isModified = false;
                }
                else
                {
                    await OnSaveAsAsync();
                }
            }
        }

        private async Task OnSaveAsAsync()
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Сохранить как...",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Текстовые файлы", Extensions = new List<string> { "txt" } }
                    }
                };
                string fileName = await saveDialog.ShowAsync(this);
                if (!string.IsNullOrEmpty(fileName))
                {
                    currentFilePath = fileName;
                    await File.WriteAllTextAsync(fileName, fileEditor.Text ?? "");
                    CurrentFileName = Path.GetFileName(fileName);
                    isModified = false;
                }
            }
        }

        private void OnExit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Методы редактирования

        private async void OnCopy(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                string textToCopy = fileEditor.SelectedText;
                if (string.IsNullOrEmpty(textToCopy))
                    textToCopy = fileEditor.Text ?? "";
                await this.Clipboard.SetTextAsync(textToCopy);
            }
        }

        private async void OnCut(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null && !string.IsNullOrEmpty(fileEditor.SelectedText))
            {
                int caretPos = fileEditor.CaretOffset;
                string cutText = fileEditor.SelectedText;
                undoStack.Push((fileEditor.Text ?? "", caretPos));
                redoStack.Clear();
                await this.Clipboard.SetTextAsync(cutText);
                fileEditor.SelectedText = "";
                lastAction = (LastActionType.Delete, cutText, caretPos);
            }
        }

        private async void OnPaste(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                var clipboardText = await this.Clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    int caret = fileEditor.CaretOffset;
                    undoStack.Push((fileEditor.Text ?? "", caret));
                    redoStack.Clear();
                    SetTextInternal(fileEditor, fileEditor.Text.Insert(caret, clipboardText));
                    fileEditor.CaretOffset = caret + clipboardText.Length;
                    // Фиксируем последнее действие – вставка
                    lastAction = (LastActionType.Insert, clipboardText, caret);
                }
            }
        }

        private void OnUndo(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null && undoStack.Count > 0)
            {
                redoStack.Push((fileEditor.Text ?? "", fileEditor.CaretOffset));
                var lastState = undoStack.Pop();
                SetTextInternal(fileEditor, lastState.text);
                fileEditor.CaretOffset = Math.Min(lastState.caretIndex, (lastState.text ?? "").Length);
            }
        }

        private void OnRedo(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null && redoStack.Count > 0)
            {
                undoStack.Push((fileEditor.Text ?? "", fileEditor.CaretOffset));
                var lastState = redoStack.Pop();
                SetTextInternal(fileEditor, lastState.text);
                fileEditor.CaretOffset = Math.Min(lastState.caretIndex, (lastState.text ?? "").Length);
            }
        }

        private void OnRepeatLastAction(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor == null)
                return;

            switch (lastAction.ActionType)
            {
                case LastActionType.Insert:
                    {
                        int caret = fileEditor.CaretOffset;
                        string newText = fileEditor.Text.Insert(caret, lastAction.Text);
                        SetTextInternal(fileEditor, newText);
                        fileEditor.CaretOffset = caret + lastAction.Text.Length;
                        break;
                    }
                case LastActionType.Delete:
                    {
                        int caret = fileEditor.CaretOffset;
                        int deleteLength = lastAction.Text.Length;
                        if (caret < fileEditor.Text.Length && (caret + deleteLength) <= fileEditor.Text.Length)
                        {
                            string newText = fileEditor.Text.Remove(caret, deleteLength);
                            SetTextInternal(fileEditor, newText);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        private void SetTextInternal(TextEditor editor, string text)
        {
            isInternalUpdate = true;
            editor.Text = text;
            lastText = text;
            isInternalUpdate = false;
        }




        #endregion



        // Метод для вызова окна со справкой
        private async void OnHelp(object sender, RoutedEventArgs e)
        {
            string helpText = "Окно справки\n\n" +
                "Функции работы с документами:\n" +
                "• Создать документ: Открытие нового пустого файла для начала работы.\n" +
                "• Открыть документ: Загрузка содержимого существующего текстового файла в редактор.\n" +
                "• Сохранить документ: Сохранение изменений в текущем файле, а также функция «Сохранить как…» для выбора нового имени или места сохранения.\n\n" +
                "Функции редактирования текста:\n" +
                "• Копировать: Копирование выделенного текста (или всего текста, если ничего не выделено) в буфер обмена.\n" +
                "• Вырезать: Удаление выделенного текста с одновременным копированием его в буфер обмена.\n" +
                "• Вставить: Вставка текста из буфера обмена в позицию курсора.\n" +
                "• Отменить действие (Undo): Возврат к предыдущему состоянию документа.\n" +
                "• Повторить последнее действие: Выполнение последней операции (вставка или удаление) повторно.\n" +
                "• Повторное действие (Redo): Отмена отменённого действия .\n\n" +
                "Дополнительные возможности:\n" +
                "• Подсветка синтаксиса: Автоматическая подсветка синтаксиса для облегчения восприятия и редактирования кода.\n" +
                "• Нумерация строк: Наличие номеров строк для удобной навигации и поиска ошибок.\n" +
                "• Вывод ошибок: Ошибки отображаются в виде таблицы, что позволяет быстро находить и устранять их.\n";

            // Создаем новое окно для справки с прокручиваемым содержимым
            Window helpWindow = new Window
            {
                Title = "Справка",
                Width = 600,
                Height = 400,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = helpText,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(10)
                    }
                }
            };

            await helpWindow.ShowDialog(this);
        }




        private async void OnShowTask1(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task1.txt");
            await ShowReportWindow(text, "Постановка задачи");
        }

        private async void OnShowTask2(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task2.txt");
            await ShowReportWindow(text, "Разработка грамматики");
        }

        private async void OnShowTask3(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task3.txt");
            await ShowReportWindow(text, "Классификация грамматики");
        }

        private async void OnShowTask4(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task4.txt");
            await ShowReportWindow(text, "Метод анализа");
        }

        private async void OnShowTask5(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task5.txt");
            await ShowReportWindow(text, "Диагностика и нейтрализация ошибок");
        }
        private async void OnShowTask6(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task6.txt");
            await ShowReportWindow(text, "Lexer.cs");
        }
        private async void OnShowTask7(object sender, RoutedEventArgs e)
        {
            string text = ReadEmbeddedTextFile("task7.txt");
            await ShowReportWindow(text, "Parser.cs");
        }


        private string ReadEmbeddedTextFile(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string fullName = assembly.GetManifestResourceNames()
                                      .FirstOrDefault(x => x.EndsWith(resourceName));
            if (fullName == null)
                return $"[Ошибка] Ресурс {resourceName} не найден";

            using (Stream stream = assembly.GetManifestResourceStream(fullName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }


        private async Task ShowReportWindow(string content, string title)
        {
            var window = new Window
            {
                Title = title,
                Width = 600,
                Height = 400,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = content,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(10)
                    }
                }
            };

            await window.ShowDialog(this);
        }



        private void OnInsertCorrectText(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                string correctText = "const pi: f32 = 3.14;";
                SetTextInternal(fileEditor, correctText);
                CurrentFileName = "Правильный текст";
                isModified = true;
            }
        }

        private void OnInsertIncorrectText(object sender, RoutedEventArgs e)
        {
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                string incorrectText = "cont pi: frfhr32 = 3.14@4;";
                SetTextInternal(fileEditor, incorrectText);
                CurrentFileName = "Неправильный текст";
                isModified = true;
            }
        }

        #region Обработка закрытия окна с диалогом сохранения

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!_forceClose && isModified)
            {
                e.Cancel = true;
                ConfirmAndClose();
            }
            else
                base.OnClosing(e);
        }

        private async void ConfirmAndClose()
        {
            var dialog = new SaveConfirmationDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dialog.ShowDialog(this);
            var result = dialog.Result;

            if (result == SaveConfirmationResult.Yes)
            {
                await OnSaveFileAsync();
                isModified = false;
                _forceClose = true;
                Close();
            }
            else if (result == SaveConfirmationResult.No)
            {
                isModified = false;
                _forceClose = true;
                Close();
            }
            // Ничего не делаем, если Cancel
        }


        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}




