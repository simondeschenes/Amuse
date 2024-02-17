using Amuse.UI.Commands;
using Amuse.UI.Models;
using OnnxStack.FeatureExtractor.Common;
using OnnxStack.StableDiffusion.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace Amuse.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for UpdateFeatureExtractorModelDialog.xaml
    /// </summary>
    public partial class UpdateFeatureExtractorModelDialog : Window, INotifyPropertyChanged
    {
        private List<string> _invalidOptions;
        private AmuseSettings _settings;
        private FeatureExtractorModelSet _modelSetResult;
        private UpdateFeatureExtractorModelSetViewModel _updateModelSet;
        private string _validationError;
        private string _controlNetFilter;

        public UpdateFeatureExtractorModelDialog(AmuseSettings settings)
        {
            _settings = settings;
            WindowCloseCommand = new AsyncRelayCommand(WindowClose);
            WindowRestoreCommand = new AsyncRelayCommand(WindowRestore);
            WindowMinimizeCommand = new AsyncRelayCommand(WindowMinimize);
            WindowMaximizeCommand = new AsyncRelayCommand(WindowMaximize);
            SaveCommand = new AsyncRelayCommand(Save, CanExecuteSave);
            CancelCommand = new AsyncRelayCommand(Cancel, CanExecuteCancel);
            _invalidOptions = _settings.FeatureExtractorModelSets.Select(x => x.Name).ToList();
            ControlNetFilters = new List<string> { "N/A" };
            ControlNetFilters.AddRange(Enum.GetNames<ControlNetType>());
            InitializeComponent();
        }

        public AmuseSettings Settings => _settings;
        public AsyncRelayCommand WindowMinimizeCommand { get; }
        public AsyncRelayCommand WindowRestoreCommand { get; }
        public AsyncRelayCommand WindowMaximizeCommand { get; }
        public AsyncRelayCommand WindowCloseCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public List<string> ControlNetFilters { get; set; }

        public UpdateFeatureExtractorModelSetViewModel UpdateModelSet
        {
            get { return _updateModelSet; }
            set { _updateModelSet = value; NotifyPropertyChanged(); }
        }

        public string ControlNetFilter
        {
            get { return _controlNetFilter; }
            set { _controlNetFilter = value; NotifyPropertyChanged(); }
        }

        public string ValidationError
        {
            get { return _validationError; }
            set { _validationError = value; NotifyPropertyChanged(); }
        }

        public FeatureExtractorModelSet ModelSetResult
        {
            get { return _modelSetResult; }
        }

        public ControlNetType? ControlNetType
        {
            get { return Enum.TryParse<ControlNetType>(_controlNetFilter, out var result) ? result : null; }
        }

        public bool ShowDialog(FeatureExtractorModelSet modelSet, ControlNetType? controlNetType)
        {
            _invalidOptions.Remove(modelSet.Name);
            UpdateModelSet = UpdateFeatureExtractorModelSetViewModel.FromModelSet(modelSet, controlNetType);
            ControlNetFilter = controlNetType == null ? "N/A" : controlNetType.ToString();
            return base.ShowDialog() ?? false;
        }

        private bool Validate()
        {
            if (_updateModelSet == null)
                return false;

            _modelSetResult = UpdateFeatureExtractorModelSetViewModel.ToModelSet(_updateModelSet);
            if (_modelSetResult == null)
                return false;

            if (_invalidOptions.Contains(_modelSetResult.Name))
            {
                ValidationError = $"Model with name '{_modelSetResult.Name}' already exists";
                return false;
            }

            if (!File.Exists(_modelSetResult.FeatureExtractorConfig.OnnxModelPath))
            {
                ValidationError = $"ContolNet model file not found";
                return false;
            }

            ValidationError = null;
            return true;
        }

        private Task Save()
        {

            if (!Validate())
                return Task.CompletedTask;

            DialogResult = true;
            return Task.CompletedTask;
        }


        private bool CanExecuteSave()
        {
            return Validate();
        }


        private Task Cancel()
        {
            _modelSetResult = null;
            DialogResult = false;
            return Task.CompletedTask;
        }


        private bool CanExecuteCancel()
        {
            return true;
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
