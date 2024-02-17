using Amuse.UI.Commands;
using Amuse.UI.Dialogs;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using OnnxStack.Core.Config;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Amuse.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl, INavigatable, INotifyPropertyChanged
    {
        private readonly ILogger<SettingsView> _logger;
        private readonly IDialogService _dialogService;
        private ControlNetModelSetViewModel _selectedControlNetModel;
        private FeatureExtractorModelSetViewModel _selectedFeatureExtractorModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsView"/> class.
        /// </summary>
        public SettingsView()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _logger = App.GetService<ILogger<SettingsView>>();
                _dialogService = App.GetService<IDialogService>();
            }


            SaveCommand = new AsyncRelayCommand(Save);

            AddControlNetModelCommand = new AsyncRelayCommand(AddControlNetModel);
            UpdateControlNetModelCommand = new AsyncRelayCommand(UpdateControlNetModel, () => SelectedControlNetModel is not null);
            RemoveControlNetModelCommand = new AsyncRelayCommand(RemoveControlNetModel, () => SelectedControlNetModel is not null);

            AddFeatureExtractorModelCommand = new AsyncRelayCommand(AddFeatureExtractorModel);
            UpdateFeatureExtractorModelCommand = new AsyncRelayCommand(UpdateFeatureExtractorModel, () => SelectedFeatureExtractorModel is not null);
            RemoveFeatureExtractorModelCommand = new AsyncRelayCommand(RemoveFeatureExtractorModel, () => SelectedFeatureExtractorModel is not null);
            InitializeComponent();
        }

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand AddControlNetModelCommand { get; }
        public AsyncRelayCommand UpdateControlNetModelCommand { get; }
        public AsyncRelayCommand RemoveControlNetModelCommand { get; }
        public AsyncRelayCommand AddFeatureExtractorModelCommand { get; }
        public AsyncRelayCommand UpdateFeatureExtractorModelCommand { get; }
        public AsyncRelayCommand RemoveFeatureExtractorModelCommand { get; }

        public AmuseSettings UISettings
        {
            get { return (AmuseSettings)GetValue(UISettingsProperty); }
            set { SetValue(UISettingsProperty, value); }
        }
        public static readonly DependencyProperty UISettingsProperty =
            DependencyProperty.Register("UISettings", typeof(AmuseSettings), typeof(SettingsView));

        public ControlNetModelSetViewModel SelectedControlNetModel
        {
            get { return _selectedControlNetModel; }
            set { _selectedControlNetModel = value; NotifyPropertyChanged(); }
        }

        public FeatureExtractorModelSetViewModel SelectedFeatureExtractorModel
        {
            get { return _selectedFeatureExtractorModel; }
            set { _selectedFeatureExtractorModel = value; NotifyPropertyChanged(); }
        }

        public Task NavigateAsync(ImageResult imageResult)
        {
            throw new NotImplementedException();
        }

        public Task NavigateAsync(VideoResultModel videoResult)
        {
            throw new NotImplementedException();
        }


        private Task Save()
        {
            try
            {
                ConfigManager.SaveConfiguration(UISettings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving configuration file, {ex.Message}");
            }
            return Task.CompletedTask;
        }

        #region ControlNet

        private async Task AddControlNetModel()
        {
            var addModelDialog = _dialogService.GetDialog<AddControlNetModelDialog>();
            if (addModelDialog.ShowDialog())
            {
                var model = new ControlNetModelSetViewModel
                {
                    Name = addModelDialog.ModelSetResult.Name,
                    ModelSet = addModelDialog.ModelSetResult
                };
                UISettings.ControlNetModelSets.Add(model);
                SelectedControlNetModel = model;
                await Save();
            }
        }


        private async Task UpdateControlNetModel()
        {
            if (SelectedControlNetModel.IsLoaded)
            {
                MessageBox.Show("Please unload model before updating", "Model In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var updateModelDialog = _dialogService.GetDialog<UpdateControlNetModelDialog>();
            if (updateModelDialog.ShowDialog(SelectedControlNetModel.ModelSet))
            {
                var modelSet = updateModelDialog.ModelSetResult;
                SelectedControlNetModel.ModelSet = modelSet;
                SelectedControlNetModel.Name = modelSet.Name;
                SelectedControlNetModel.NotifyPropertyChanged("Type");
                await Save();
            }
        }


        private async Task RemoveControlNetModel()
        {
            if (SelectedControlNetModel.IsLoaded)
            {
                MessageBox.Show("Please unload model before uninstalling", "Model In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UISettings.ControlNetModelSets.Remove(SelectedControlNetModel);
            SelectedControlNetModel = UISettings.ControlNetModelSets.FirstOrDefault();
            await Save();
        }

        #endregion

        #region Feature Extractor

        private async Task AddFeatureExtractorModel()
        {
            var addModelDialog = _dialogService.GetDialog<AddFeatureExtractorModelDialog>();
            if (addModelDialog.ShowDialog())
            {
                var model = new FeatureExtractorModelSetViewModel
                {
                    Name = addModelDialog.ModelSetResult.Name,
                    ModelSet = addModelDialog.ModelSetResult
                };
                UISettings.FeatureExtractorModelSets.Add(model);
                SelectedFeatureExtractorModel = model;
                await Save();
            }
        }


        private async Task UpdateFeatureExtractorModel()
        {
            if (SelectedFeatureExtractorModel.IsLoaded)
            {
                MessageBox.Show("Please unload model before updating", "Model In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var updateModelDialog = _dialogService.GetDialog<UpdateFeatureExtractorModelDialog>();
            if (updateModelDialog.ShowDialog(SelectedFeatureExtractorModel.ModelSet, SelectedFeatureExtractorModel.ControlNetType))
            {
                var modelSet = updateModelDialog.ModelSetResult;
                SelectedFeatureExtractorModel.ModelSet = modelSet;
                SelectedFeatureExtractorModel.Name = modelSet.Name;
                SelectedFeatureExtractorModel.ControlNetType = updateModelDialog.ControlNetType;
                await Save();
            }
        }


        private async Task RemoveFeatureExtractorModel()
        {
            if (SelectedFeatureExtractorModel.IsLoaded)
            {
                MessageBox.Show("Please unload model before uninstalling", "Model In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UISettings.FeatureExtractorModelSets.Remove(SelectedFeatureExtractorModel);
            SelectedFeatureExtractorModel = UISettings.FeatureExtractorModelSets.FirstOrDefault();
            await Save();
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
