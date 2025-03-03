using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using System.Collections.Generic;
using System;
using TFLandCOMP.ViewModels;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Styling;
using TFLandCOMP.Services;
using FluentAvalonia.UI.Navigation;
using Avalonia.Media.Imaging;
using Avalonia.Media;

using Microsoft.VisualBasic;
using Avalonia.VisualTree;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace TFLandCOMP.Views
{
    
    public partial class MainView : UserControl
    {
        private bool _isDesktop;
        //private NavigationView NavView;
        //private Frame FrameView;
        private Control OverlayHost;


        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewViewModel();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            ClipboardService.Owner = TopLevel.GetTopLevel(this);

            _isDesktop = TopLevel.GetTopLevel(this) is Window;

           

        }

        

        
    }




}
