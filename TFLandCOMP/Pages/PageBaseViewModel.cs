using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFLandCOMP.ViewModels;


namespace TFLandCOMP.Pages
{
    public class PageBaseViewModel
    {
        public PageBaseViewModel()
        {
            
        }

        public MainPageViewModelBase Parent { get; set; }

        public string Header { get; set; }

        public string Description { get; set; }

        public string IconResourceKey { get; set; }

        public string PageKey { get; set; }

        public string[] SearchKeywords { get; set; }

        

        private void PageInvoked(object param)
        {
          
        }
    }
}
