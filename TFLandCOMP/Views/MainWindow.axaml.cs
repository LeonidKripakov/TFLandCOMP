using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaEdit;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using TFLandCOMP.ViewModels;

namespace TFLandCOMP.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Поле для текущего пути файла (если null – файл новый)
        private string currentFilePath = null;

        // Свойство для отображения имени файла
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

        // Стэки для Undo/Redo (хранят текущее состояние текста и позицию каретки)
        private Stack<(string text, int caretIndex)> undoStack = new Stack<(string text, int caretIndex)>();
        private Stack<(string text, int caretIndex)> redoStack = new Stack<(string text, int caretIndex)>();

        // Флаги для отслеживания изменений
        private bool isInternalUpdate = false;
        private bool isModified = false;
        private string lastText = "";

        // Флаг для принудительного закрытия окна (после подтверждения сохранения)
        private bool _forceClose = false;

        // Перечисление типов последнего действия (вставка или удаление)
        private enum LastActionType { None, Insert, Delete }
        // Фиксируем последнее действие: тип, текст изменения и позицию
        private (LastActionType ActionType, string Text, int Position) lastAction = (LastActionType.None, "", 0);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewViewModel();
            // Получаем редактор из XAML (AvaloniaEdit)
            var fileEditor = this.FindControl<TextEditor>("fileTextEditor");
            if (fileEditor != null)
            {
                fileEditor.TextChanged += (s, e) =>
                {
                    var newText = fileEditor.Text;
                    if (!isInternalUpdate)
                    {
                        if (lastText != newText)
                        {
                            // Сохраняем текущее состояние для Undo/Redo
                            undoStack.Push((lastText, fileEditor.CaretOffset));
                            redoStack.Clear();
                            isModified = true;

                            // Если текст увеличился – фиксируем вставку, если уменьшился – удаление
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

                // Изменение размера шрифта с помощью колесика мыши при зажатом Ctrl
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
                // Фиксируем последнее действие – удаление выбранного блока
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

        // Метод повторения последнего действия (вставка или удаление)
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
