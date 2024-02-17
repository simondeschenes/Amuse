﻿using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using OnnxStack.FeatureExtractor.Common;
using OnnxStack.StableDiffusion.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace Amuse.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for AddFeatureExtractorModelDialog.xaml
    /// </summary>
    public partial class AddFeatureExtractorModelDialog : Window, INotifyPropertyChanged
    {
        private readonly ILogger<AddFeatureExtractorModelDialog> _logger;

        private readonly List<string> _invalidOptions;
        private string _modelName;
        private string _modelFile;
        private bool _normalize;
        private int _sampleSize = 512;
        private int _channels = 1;
        private string _controlNetFilter = "N/A";
        private IModelFactory _modelFactory;
        private AmuseSettings _settings;
        private FeatureExtractorModelSet _modelSetResult;

        public AddFeatureExtractorModelDialog(AmuseSettings settings, IModelFactory modelFactory, ILogger<AddFeatureExtractorModelDialog> logger)
        {
            _logger = logger;
            _settings = settings;
            _modelFactory = modelFactory;
            WindowCloseCommand = new AsyncRelayCommand(WindowClose);
            WindowRestoreCommand = new AsyncRelayCommand(WindowRestore);
            WindowMinimizeCommand = new AsyncRelayCommand(WindowMinimize);
            WindowMaximizeCommand = new AsyncRelayCommand(WindowMaximize);
            SaveCommand = new AsyncRelayCommand(Save, CanExecuteSave);
            CancelCommand = new AsyncRelayCommand(Cancel);
            _invalidOptions = _settings.FeatureExtractorModelSets.Select(x => x.Name).ToList();
            ControlNetFilters = ["N/A", .. Enum.GetNames<ControlNetType>()];
            InitializeComponent();
        }

        public AmuseSettings Settings => _settings;
        public AsyncRelayCommand WindowMinimizeCommand { get; }
        public AsyncRelayCommand WindowRestoreCommand { get; }
        public AsyncRelayCommand WindowMaximizeCommand { get; }
        public AsyncRelayCommand WindowCloseCommand { get; }

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public ObservableCollection<ValidationResult> ValidationResults { get; set; } = new ObservableCollection<ValidationResult>();

        public List<string> ControlNetFilters { get; set; }

        public string ModelName
        {
            get { return _modelName; }
            set { _modelName = value; _modelName?.Trim(); NotifyPropertyChanged(); CreateModelSet(); }
        }

        public string ModelFile
        {
            get { return _modelFile; }
            set
            {
                _modelFile = value;
                _modelName = string.IsNullOrEmpty(_modelFile)
                    ? string.Empty
                    : Path.GetFileNameWithoutExtension(_modelFile);

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ModelName));
                CreateModelSet();
            }
        }

        public string ControlNetFilter
        {
            get { return _controlNetFilter; }
            set { _controlNetFilter = value; NotifyPropertyChanged(); }
        }

        public int SampleSize
        {
            get { return _sampleSize; }
            set { _sampleSize = value; NotifyPropertyChanged(); CreateModelSet(); }
        }

        public bool Normalize
        {
            get { return _normalize; }
            set { _normalize = value; NotifyPropertyChanged(); CreateModelSet(); }
        }

        public int Channels
        {
            get { return _channels; }
            set { _channels = value; NotifyPropertyChanged(); CreateModelSet(); }
        }

        public FeatureExtractorModelSet ModelSetResult
        {
            get { return _modelSetResult; }
        }

        public ControlNetType? ControlNetType
        {
            get { return Enum.TryParse<ControlNetType>(_controlNetFilter, out var result) ? result : null; }
        }


        public new bool ShowDialog()
        {
            return base.ShowDialog() ?? false;
        }


        private void CreateModelSet()
        {
            _modelSetResult = null;
            ValidationResults.Clear();
            if (string.IsNullOrEmpty(_modelFile))
                return;

            _modelSetResult = _modelFactory.CreateFeatureExtractorModelSet(ModelName.Trim(), _normalize, _sampleSize, _channels, _modelFile);

            // Validate
            ValidationResults.Add(new ValidationResult("Name", !_invalidOptions.Contains(_modelName, StringComparer.OrdinalIgnoreCase) && _modelName.Length > 2 && _modelName.Length < 50));
            ValidationResults.Add(new ValidationResult("Model", File.Exists(_modelSetResult.FeatureExtractorConfig.OnnxModelPath)));
        }


        private Task Save()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }


        private bool CanExecuteSave()
        {
            if (string.IsNullOrEmpty(_modelFile))
                return false;
            if (_modelSetResult is null)
                return false;

            return ValidationResults.Count > 0 && ValidationResults.All(x => x.IsValid);
        }


        private Task Cancel()
        {
            _modelSetResult = null;
            DialogResult = false;
            return Task.CompletedTask;
        }


        #region BaseWindow

        private Task WindowClose()
        {
            Close();
            return Task.CompletedTask;
        }

        private Task WindowRestore()
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
            return Task.CompletedTask;
        }

        private Task WindowMinimize()
        {
            WindowState = WindowState.Minimized;
            return Task.CompletedTask;
        }

        private Task WindowMaximize()
        {
            WindowState = WindowState.Maximized;
            return Task.CompletedTask;
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }
}
