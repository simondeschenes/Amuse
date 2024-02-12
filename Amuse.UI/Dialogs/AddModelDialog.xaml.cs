﻿using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using OnnxStack.StableDiffusion.Config;
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
    /// Interaction logic for AddModelDialog.xaml
    /// </summary>
    public partial class AddModelDialog : Window, INotifyPropertyChanged
    {
        private readonly ILogger<AddModelDialog> _logger;

        private List<string> _invalidOptions;
        private string _modelFolder;
        private string _modelName;
        private IModelFactory _modelFactory;
        private AmuseSettings _settings;
        private ModelTemplateViewModel _modelTemplate;
        private StableDiffusionModelSet _modelSetResult;

        public AddModelDialog(AmuseSettings settings, IModelFactory modelFactory, ILogger<AddModelDialog> logger)
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
            ModelTemplates = _settings.Templates.Where(x => !x.IsUserTemplate).ToList();
            _invalidOptions = _settings.GetModelNames();
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
        public List<ModelTemplateViewModel> ModelTemplates { get; set; }

        public ModelTemplateViewModel ModelTemplate
        {
            get { return _modelTemplate; }
            set { _modelTemplate = value; NotifyPropertyChanged(); CreateModelSet(); }
        }

        public string ModelName
        {
            get { return _modelName; }
            set { _modelName = value; _modelName?.Trim(); NotifyPropertyChanged(); CreateModelSet(); }
        }

        public string ModelFolder
        {
            get { return _modelFolder; }
            set
            {
                _modelFolder = value;
                if (_modelTemplate is not null && !_modelTemplate.IsUserTemplate)
                    _modelName = string.IsNullOrEmpty(_modelFolder)
                        ? string.Empty
                        : Path.GetFileName(_modelFolder);

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ModelName));
                CreateModelSet();
            }
        }

        public StableDiffusionModelSet ModelSetResult
        {
            get { return _modelSetResult; }
        }

        private bool _enableTemplateSelection = true;

        public bool EnableTemplateSelection
        {
            get { return _enableTemplateSelection; }
            set { _enableTemplateSelection = value; NotifyPropertyChanged(); }
        }

        private bool _enableNameSelection = true;
        public bool EnableNameSelection
        {
            get { return _enableNameSelection; }
            set { _enableNameSelection = value; NotifyPropertyChanged(); }
        }


        public bool ShowDialog(ModelTemplateViewModel selectedTemplate = null)
        {
            if (selectedTemplate is not null)
            {
                EnableNameSelection = !selectedTemplate.IsUserTemplate;
                EnableTemplateSelection = false;
                ModelTemplate = selectedTemplate;
                ModelName = selectedTemplate.IsUserTemplate ? selectedTemplate.Name : string.Empty;
            }
            return base.ShowDialog() ?? false;
        }


        private void CreateModelSet()
        {
            _modelSetResult = null;
            ValidationResults.Clear();
            if (string.IsNullOrEmpty(_modelFolder))
                return;

            _modelSetResult = _modelFactory.CreateStableDiffusionModelSet(ModelName.Trim(), ModelFolder, _modelTemplate.StableDiffusionTemplate);

            // Validate
            if (_enableNameSelection)
                ValidationResults.Add(new ValidationResult("Name", !_invalidOptions.Contains(_modelName.ToLower()) && _modelName.Length > 2 && _modelName.Length < 50));

            ValidationResults.Add(new ValidationResult("Unet Model", File.Exists(_modelSetResult.UnetConfig.OnnxModelPath)));
            ValidationResults.Add(new ValidationResult("Tokenizer Model", File.Exists(_modelSetResult.TokenizerConfig.OnnxModelPath)));
            if (_modelSetResult.Tokenizer2Config is not null)
                ValidationResults.Add(new ValidationResult("Tokenizer2 Model", File.Exists(_modelSetResult.Tokenizer2Config.OnnxModelPath)));
            ValidationResults.Add(new ValidationResult("TextEncoder Model", File.Exists(_modelSetResult.TextEncoderConfig.OnnxModelPath)));
            if (_modelSetResult.TextEncoder2Config is not null)
                ValidationResults.Add(new ValidationResult("TextEncoder2 Model", File.Exists(_modelSetResult.TextEncoder2Config.OnnxModelPath)));
            ValidationResults.Add(new ValidationResult("VaeDecoder Model", File.Exists(_modelSetResult.VaeDecoderConfig.OnnxModelPath)));
            ValidationResults.Add(new ValidationResult("VaeEncoder Model", File.Exists(_modelSetResult.VaeEncoderConfig.OnnxModelPath)));
        }


        private Task Save()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }


        private bool CanExecuteSave()
        {
            if (string.IsNullOrEmpty(_modelFolder))
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
