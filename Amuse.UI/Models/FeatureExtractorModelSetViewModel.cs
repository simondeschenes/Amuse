﻿using OnnxStack.FeatureExtractor.Common;
using OnnxStack.StableDiffusion.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Amuse.UI.Models
{
    public class FeatureExtractorModelSetViewModel : INotifyPropertyChanged
    {
        private string _name;
        private ControlNetType? _controlNetType;
        private bool _isLoaded;
        private bool _isLoading;

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged(); }
        }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ControlNetType? ControlNetType
        {
            get { return _controlNetType; }
            set { _controlNetType = value; NotifyPropertyChanged(); }
        }


        [JsonIgnore]
        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { _isLoaded = value; NotifyPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; NotifyPropertyChanged(); }
        }

        public FeatureExtractorModelSet ModelSet { get; set; }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }
}
