using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TFLandCOMP.Models;
using TFLandCOMP.Services; // Здесь должен быть ваш статический класс Patterns

namespace TFLandCOMP.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {
        // Коллекция для ошибок (RunScan)
        public ObservableCollection<ErrorDetail> Errors { get; } = new ObservableCollection<ErrorDetail>();

        // Новая коллекция для результатов поиска по регуляркам
        public ObservableCollection<MatchResult> Matches { get; } = new ObservableCollection<MatchResult>();

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        // Команды
        public IRelayCommand RunScanCommand { get; }
        public IRelayCommand FindWordsNotEndingWithTCommand { get; }
        public IRelayCommand FindUsernamesCommand { get; }
        public IRelayCommand FindLongitudeCommand { get; }

        public MainViewViewModel()
        {
            RunScanCommand = new RelayCommand(RunScan);
            FindWordsNotEndingWithTCommand = new RelayCommand(FindWordsNotEndingWithT);
            FindUsernamesCommand = new RelayCommand(FindUsernames);
            FindLongitudeCommand = new RelayCommand(FindLongitude);
        }

        // Существующий метод разбора лексера/парсера
        private void RunScan()
        {
            Errors.Clear();
            if (string.IsNullOrWhiteSpace(InputText))
                return;

            var lexer = new Lexer();
            var parser = new Parser();
            var tokens = lexer.Lex(InputText);
            var parseErrors = parser.ParseDeclarations(tokens, InputText);

            if (parseErrors.Count == 0)
            {
                Errors.Add(new ErrorDetail
                {
                    ErrorCode = "OK",
                    ErrorMessage = "Ошибок не обнаружено",
                    Position = ""
                });
            }
            else
            {
                foreach (var error in parseErrors)
                    Errors.Add(error);
            }
        }

        // Универсальный метод заполнения Matches
        private void FindWithRegex(Regex rx)
        {
            Matches.Clear();

            if (string.IsNullOrWhiteSpace(InputText))
                return;

            foreach (Match m in rx.Matches(InputText))
            {
                Matches.Add(new MatchResult
                {
                    Value = m.Value,
                    Index = m.Index
                });
            }
        }

        // Обработчики для трёх задач
        private void FindWordsNotEndingWithT()
            => FindWithRegex(Patterns.WordsNotEndingWithT);

        private void FindUsernames()
            => FindWithRegex(Patterns.Username);

        private void FindLongitude()
            => FindWithRegex(Patterns.Longitude);
    }

    // Модель для вывода результатов поиска
    public class MatchResult
    {
        public string Value { get; set; }
        public int Index { get; set; }
    }
}
