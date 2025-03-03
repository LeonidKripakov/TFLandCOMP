using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace TFLandCOMP.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected string GetAssemblyResource(string name)
        {
            using (var stream = AssetLoader.Open(new Uri(name)))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        protected void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }





    public class MainPageViewModelBase : ViewModelBase
    {
        public string NavHeader { get; set; }

        public string IconKey { get; set; }

        public bool ShowsInFooter { get; set; }
    }
}
