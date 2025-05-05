using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;


using TFLandCOMP.Models;


namespace TFLandCOMP.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {
        public ObservableCollection<ErrorDetail> Errors { get; set; } = new ObservableCollection<ErrorDetail>();

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

        // Использует Lexer и Parser для анализа входного текста.
        private void RunScan()
        {
            Errors.Clear();

            if (string.IsNullOrWhiteSpace(InputText))
                return;

            Lexer lexer = new Lexer();
            Parser parser = new Parser();

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
                {
                    Errors.Add(error);
                }
            }
        }

    }
}
