using System;
using System.Collections.Generic;
using Torch;

namespace DistressCallPlugin
{
    public class DistressCallConfig : ViewModel
    {

        //private string _StringProperty = "root";
        //private int _IntProperty = 0;
        //private bool _BoolProperty = true;
        private bool _Enabled = true;

        //public string StringProperty { get => _StringProperty; set => SetValue(ref _StringProperty, value); }
        //public int IntProperty { get => _IntProperty; set => SetValue(ref _IntProperty, value); }
        //public bool BoolProperty { get => _BoolProperty; set => SetValue(ref _BoolProperty, value); }
        public bool Enabled { get => _Enabled; set => SetValue(ref _Enabled, value); }
    }
}
