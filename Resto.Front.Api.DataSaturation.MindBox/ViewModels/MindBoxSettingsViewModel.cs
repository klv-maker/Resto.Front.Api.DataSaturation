using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces.VIewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.MindBox.ViewModels
{
    public class MindBoxSettingsViewModel : IMindBoxSettingsViewModel
    {
        public Action CloseAction { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public MindBoxSettingsViewModel(IMindBoxSettings mindBoxSettings) 
        { 
        }

    }
}
