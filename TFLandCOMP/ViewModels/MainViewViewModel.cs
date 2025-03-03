using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;


namespace TFLandCOMP.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {

        public ObservableCollection<string> Results { get; set; } = new ObservableCollection<string>();

        public MainViewViewModel()
        {
            
            


        }


    }

}
