using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

using TFLandCOMP.Models;
using TFLandCOMP.Services;

namespace TFLandCOMP.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {
        public ObservableCollection<ErrorDetail> Errors { get; set; } = new ObservableCollection<ErrorDetail>();
        public ObservableCollection<string> Quadruples { get; set; } = new ObservableCollection<string>();

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public IRelayCommand RunScanCommand { get; }

        public MainViewViewModel()
        {
            RunScanCommand = new RelayCommand(RunScan);
        }

        private void RunScan()
        {
            Errors.Clear();
            Quadruples.Clear();

            if (string.IsNullOrWhiteSpace(InputText))
                return;

            var lexer = new Lexer();
            var tokens = lexer.Lex(InputText);

            var parser = new ExpressionParser();
            var parseErrors = parser.Parse(tokens, InputText);

            if (parseErrors.Count == 0)
            {
                Errors.Add(new ErrorDetail
                {
                    ErrorCode = "OK",
                    ErrorMessage = "Ошибок не обнаружено",
                    Position = ""
                });

                var quadGen = new QuadrupleGenerator();
                var quads = quadGen.Generate(tokens);

                // корректная сортировка по приоритету операций
                foreach (var q in quads)
                {
                    Quadruples.Add(q.ToString());
                }
            }
            else
            {
                foreach (var error in parseErrors)
                {
                    Errors.Add(error);
                }
            }
        }
    }
}
