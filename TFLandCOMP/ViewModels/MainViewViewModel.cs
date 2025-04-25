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
            Lexer lexer = new Lexer();
            var tokens = lexer.Lex(InputText);

            if (tokens.Count == 0)
            {
                Errors.Add(new ErrorDetail
                {
                    ErrorCode = "EMPTY",
                    ErrorMessage = "Лексемы не найдены",
                    Position = ""
                });
            }
            else
            {
                foreach (var token in tokens)
                {
                    Errors.Add(new ErrorDetail
                    {
                        ErrorCode = token.Type.ToString(),
                        ErrorMessage = $"Значение: '{token.Value}'",
                        Position = $"Позиция: {token.StartIndex}"
                    });
                }
            }
        }
    }
}
