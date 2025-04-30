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
            string text = "1. Постановка задачи\n\nКонстанты – это элементы данных..." +
                          "1 Постановка задачи\r\nКонстанты – это элементы данных, значения которых известны и в процессе выполнения программы не изменяются.\r\nДля описания констант в языке Rust используется служебное слово \"const\".\r\nФормат записи: \"const имя_константы: тип = значение;\".\r\nПримеры допустимых констант:\r\n•\tЦелая константа: const MAX_VALUE: i32 = 128;\r\n•\tВещественная константа с фиксированной точкой: const MIN_VALUE: f32 = -39.1;\r\nПарсер, реализованный в курсовой работе на основе разработанной грамматики, будет корректно обрабатывать также следующие записи:\r\n•\tconst a: f32 = .001;\r\n•\tconst c: f32 = 001.123;\r\n\r\n";
            await ShowReportWindow(text, "Постановка задачи");
        }

        private async void OnShowTask2(object sender, RoutedEventArgs e)
        {
            string text = "2. Разработка грамматики\n\nОпределим грамматику объявлений..." +
                          " Определим грамматику объявлений констант языка Rust G[‹Def›] в нотации Хомского с продукциями P:\r\n•\t‹Def› → const ‹Id› : ‹Type› = ‹Number› ;\r\n•\t‹Id› → ‹Letter› ‹IdRem›\r\n•\t‹IdRem› → ‹Letter› ‹IdRem› | ε\r\n•\t‹Type› → f32 | f64\r\n•\t‹Number› → [+|-]‹UnsignedNumber›\r\n•\t‹UnsignedNumber› → ‹Decimal› [E ‹Integer›] | E ‹Integer›\r\n•\t‹Decimal› → [‹UnsignedInt›] . ‹UnsignedInt› | ‹UnsignedInt›\r\n•\t‹Integer› → [+|-] ‹UnsignedInt›\r\n•\t‹UnsignedInt› → ‹Digit›{‹Digit›}\r\n•\tДополнительные определения:\r\n•\t‹Digit› → \"0\" | \"1\" | \"2\" | \"3\" | \"4\" | \"5\" | \"6\" | \"7\" | \"8\" | \"9\"\r\n•\t‹Letter› → \"a\" | \"b\" | \"c\" | ... | \"z\" | \"A\" | \"B\" | \"C\" | ... | \"Z\"\r\n•\tСледуя этому формальному описанию, представим компоненты грамматики G[‹Def›]:\r\n•\tZ = ‹Def›\r\n•\tVT = {const, :, f32, f64, =, ;, ., +, -, 0-9, a-z, A-Z}\r\n•\tVN = {‹Def›, ‹Id›, ‹IdRem›, ‹Type›, ‹Number›, ‹UnsignedNumber›, ‹Decimal›, ‹Integer›, ‹UnsignedInt›}\r\n";
            await ShowReportWindow(text, "Разработка грамматики");
        }

        private async void OnShowTask3(object sender, RoutedEventArgs e)
        {
            string text = "3. Классификация грамматики\n\nГрамматика G[‹Def›] является автоматной..." +
                          " Согласно классификации Хомского, грамматика G[‹Def›] является автоматной.\r\nПравила (1)-(7) относятся к классу праворекурсивных продукций (A → aB | a | ε):\r\n1.\t‹Def› → ‹Letter›‹IdRem›\r\n2.\t‹IdRem› → ‹Letter›‹IdRem›\r\n3.\t‹IdRem› → _‹Name›\r\n4.\t‹Name› → ‹Letter›‹NameRem›\r\n5.\t‹NameRem› → ‹Letter›‹NameRem›\r\n6.\t‹NameRem› → =‹Number›\r\n7.\t‹Number› → [+|-]‹UnsignedNumber›;\r\nОтметим, что правила должны быть либо только леворекурсивными, либо только праворекурсивными. Комбинация тех и других не допускается. Однако данная грамматика содержит одновременно леворекурсивные и праворекурсивные продукции (8)-(11), и, следовательно, не является полностью автоматной.\r\n8.\t‹UnsignedNumber› → ‹Decimal›[E‹Integer›] | E‹Integer›\r\n9.\t‹Decimal› → [‹UnsignedInt›].‹UnsignedInt› | ‹UnsignedInt›\r\n10.\t‹Integer› → [+|-]‹UnsignedInt›\r\n11.\t‹UnsignedInt› → ‹Digit›{‹Digit›}\r\n";
            await ShowReportWindow(text, "Классификация грамматики");
        }

        private async void OnShowTask4(object sender, RoutedEventArgs e)
        {
            string text = "4. Метод анализа\n\nГрамматика реализуется на графе..." +
                          " Грамматика G[‹Def›] является автоматной.\r\nПравила (1) – (11) для G[‹Def›] реализованы на графе .\r\nСплошные стрелки на графе характеризуют синтаксически верный разбор; пунктирные символизируют состояние ошибки (ERROR); дуга λ и непомеченные дуги предполагают любой терминальный символ, отличный от указанного из соответствующего узла.\r\n";
            await ShowReportWindow(text, "Метод анализа");
        }

        private async void OnShowTask5(object sender, RoutedEventArgs e)
        {
            string text = "5. Диагностика и нейтрализация синтаксических ошибок\n\nМетод Айронса..." +
                          " Согласно заданию на курсовую работу, необходимо реализовать нейтрализацию синтаксических ошибок, используя метод Айронса.\r\n5.1 Метод Айронса\r\nСуть метода Айронса заключается в следующем:\r\nПри обнаружении ошибки (во входной цепочке в процессе разбора встречается символ, который не соответствует ни одному из ожидаемых символов), входная цепочка символов выглядит следующим образом: Tt, где T – следующий символ во входном потоке (ошибочный символ), t – оставшаяся во входном потоке цепочка символов после T. Алгоритм нейтрализации состоит из следующих шагов:\r\n1. Определяются недостроенные кусты дерева разбора;\r\n2. Формируется множество L – множество остаточных символов недостроенных кустов дерева разбора;\r\n3. Из входной цепочки удаляется следующий символ до тех пор, пока цепочка не примет вид Tt, такой, что U => T, где U ∈ L, то есть до тех пор, пока следующий в цепочке символ T не сможет быть выведен из какого-нибудь из остаточных символов недостроенных кустов.\r\n4. Определяется, какой из недостроенных кустов стал причиной появления символа U в множестве L (иначе говоря, частью какого из недостроенных кустов является символ U).\r\nТаким образом, определяется, к какому кусту в дереве разбора можно «привязать» оставшуюся входную цепочку символов после удаления из текста ошибочного фрагмента.\r\n5.2 Метод Айронса для автоматной грамматики\r\nРазрабатываемый синтаксический анализатор построен на базе автоматной грамматики. Реализация алгоритма Айронса для автоматной грамматики имеет следующую особенность.\r\nДерево разбора с использованием автоматной грамматики представлено на рисунке 2.\r\n \r\nТаким образом, при возникновении синтаксической ошибки в процессе разбора с использованием автоматной грамматики, в дереве разбора всегда будет только один недостроенный куст \r\nПоскольку единственный недостроенный куст – это тот, во время построения которого возникла синтаксическая ошибка, то это единственный куст, к которому можно привязать оставшуюся входную цепочку символов.\r\nПредлагается свести алгоритм нейтрализации к последовательному удалению следующего символа во входной цепочке до тех пор, пока следующий символ не окажется одним из допустимых в данный момент разбора.\r\n \r\n";
            await ShowReportWindow(text, "Диагностика и нейтрализация ошибок");
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
                string incorrectText = "constpi:f32=3.14;;";
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
            var result = await dialog.ShowDialog<SaveConfirmationResult>(this);
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

                Close();
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}




