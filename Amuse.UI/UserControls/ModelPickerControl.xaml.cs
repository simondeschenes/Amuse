using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using OnnxStack.Core;
using OnnxStack.StableDiffusion.Common;
using OnnxStack.StableDiffusion.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Amuse.UI.UserControls
{
    /// <summary>
    /// Interaction logic for Parameters.xaml
    /// </summary>
    public partial class ModelPickerControl : UserControl, INotifyPropertyChanged
    {
        private readonly ILogger<ModelPickerControl> _logger;
        private readonly IStableDiffusionService _stableDiffusionService;
        private ICollectionView _modelCollectionView;
        private ICollectionView _controlNetModelCollectionView;

        /// <summary>Initializes a new instance of the <see cref="ModelPickerControl" /> class.</summary>
        public ModelPickerControl()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _logger = App.GetService<ILogger<ModelPickerControl>>();
                _stableDiffusionService = App.GetService<IStableDiffusionService>();
            }

            LoadCommand = new AsyncRelayCommand<StableDiffusionModelSetViewModel>(LoadModel);
            UnloadCommand = new AsyncRelayCommand<StableDiffusionModelSetViewModel>(UnloadModel);
            LoadControlNetCommand = new AsyncRelayCommand<ControlNetModelSetViewModel>(LoadControlNetModel);
            UnloadControlNetCommand = new AsyncRelayCommand<ControlNetModelSetViewModel>(UnloadControlNetModel);
            InitializeComponent();
        }

        public static readonly DependencyProperty SettingsProperty =
            DependencyProperty.Register("Settings", typeof(AmuseSettings), typeof(ModelPickerControl), new PropertyMetadata(OnSettingsChangedCalback()));

        public static readonly DependencyProperty SupportedDiffusersProperty =
            DependencyProperty.Register("SupportedDiffusers", typeof(List<DiffuserType>), typeof(ModelPickerControl), new PropertyMetadata(OnSupportedDiffusersChangedCallback()));

        public static readonly DependencyProperty SelectedModelProperty =
            DependencyProperty.Register("SelectedModel", typeof(StableDiffusionModelSetViewModel), typeof(ModelPickerControl), new PropertyMetadata(OnSelectedModelChangedCallback()));

        public static readonly DependencyProperty SelectedControlNetModelProperty =
            DependencyProperty.Register("SelectedControlNetModel", typeof(ControlNetModelSetViewModel), typeof(ModelPickerControl));


        /// <summary>
        /// Gets or sets the load StableDiffusion model command.
        /// </summary>
        public AsyncRelayCommand<StableDiffusionModelSetViewModel> LoadCommand { get; set; }

        /// <summary>
        /// Gets or sets the unload StableDiffusion model command.
        /// </summary>
        public AsyncRelayCommand<StableDiffusionModelSetViewModel> UnloadCommand { get; set; }

        /// <summary>
        /// Gets or sets the load ControlNet model command.
        /// </summary>
        public AsyncRelayCommand<ControlNetModelSetViewModel> LoadControlNetCommand { get; set; }

        /// <summary>
        /// Gets or sets the unload ControlNet model command.
        /// </summary>
        public AsyncRelayCommand<ControlNetModelSetViewModel> UnloadControlNetCommand { get; set; }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public AmuseSettings Settings
        {
            get { return (AmuseSettings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the supported diffusers.
        /// </summary>
        public List<DiffuserType> SupportedDiffusers
        {
            get { return (List<DiffuserType>)GetValue(SupportedDiffusersProperty); }
            set { SetValue(SupportedDiffusersProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selected model.
        /// </summary>
        public StableDiffusionModelSetViewModel SelectedModel
        {
            get { return (StableDiffusionModelSetViewModel)GetValue(SelectedModelProperty); }
            set { SetValue(SelectedModelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selected ControlNet model.
        /// </summary>
        public ControlNetModelSetViewModel SelectedControlNetModel
        {
            get { return (ControlNetModelSetViewModel)GetValue(SelectedControlNetModelProperty); }
            set { SetValue(SelectedControlNetModelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the model collection view.
        /// </summary>
        public ICollectionView ModelCollectionView
        {
            get { return _modelCollectionView; }
            set { _modelCollectionView = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the ControlNet model collection view.
        /// </summary>
        public ICollectionView ControlNetModelCollectionView
        {
            get { return _controlNetModelCollectionView; }
            set { _controlNetModelCollectionView = value; NotifyPropertyChanged(); }
        }


        /// <summary>
        /// Loads the model.
        /// </summary>
        private async Task LoadModel(StableDiffusionModelSetViewModel stableDiffusionModel)
        {
            if (_stableDiffusionService.IsModelLoaded(stableDiffusionModel.ModelSet))
                return;

            var elapsed = _logger.LogBegin($"StableDiffusion model '{stableDiffusionModel.Name}' Loading...");
            stableDiffusionModel.IsLoaded = false;
            stableDiffusionModel.IsLoading = true;

            try
            {
                if (Settings.ModelCacheMode == ModelCacheMode.Single)
                {
                    foreach (var model in Settings.StableDiffusionModelSets.Where(x => x.IsLoaded))
                    {
                        await UnloadModel(model);
                    }
                }
                stableDiffusionModel.IsLoaded = await _stableDiffusionService.LoadModelAsync(stableDiffusionModel.ModelSet);
                _logger.LogEnd($"StableDiffusion model '{stableDiffusionModel.Name}' Loaded.", elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured while loading StableDiffusion model  '{Name}'\nException Message: {Message}", stableDiffusionModel.Name, ex.Message);
            }

            stableDiffusionModel.IsLoading = false;
        }


        /// <summary>
        /// Unloads the model.
        /// </summary>
        private async Task UnloadModel(StableDiffusionModelSetViewModel stableDiffusionModel)
        {
            if (!_stableDiffusionService.IsModelLoaded(stableDiffusionModel.ModelSet))
                return;

            _logger.LogInformation("StableDiffusion model '{stableDiffusionModel.Name}' Unloading...", stableDiffusionModel.Name);
            stableDiffusionModel.IsLoading = true;
            if (stableDiffusionModel.IsControlNet)
            {
                _logger.LogInformation("Unloading ControlNet models...");
                foreach (var controlNetModel in Settings.ControlNetModelSets.Where(x => x.IsLoaded))
                {
                    await UnloadControlNetModel(controlNetModel);
                }
            }

            await _stableDiffusionService.UnloadModelAsync(stableDiffusionModel.ModelSet);
            stableDiffusionModel.IsLoading = false;
            stableDiffusionModel.IsLoaded = false;
            _logger.LogInformation("StableDiffusion model '{stableDiffusionModel.Name}' Unloaded.", stableDiffusionModel.Name);
        }


        /// <summary>
        /// Loads a ControlNet model.
        /// </summary>
        /// <param name="controlNetModel">The control net model.</param>
        private async Task LoadControlNetModel(ControlNetModelSetViewModel controlNetModel)
        {
            if (_stableDiffusionService.IsControlNetModelLoaded(controlNetModel.ModelSet))
                return;

            var elapsed = _logger.LogBegin($"ControlNet model '{controlNetModel.Name}' Loading...");
            controlNetModel.IsLoaded = false;
            controlNetModel.IsLoading = true;

            try
            {
                if (Settings.ModelCacheMode == ModelCacheMode.Single)
                {
                    foreach (var loadedControlNetModel in Settings.ControlNetModelSets.Where(x => x.IsLoaded))
                    {
                        await UnloadControlNetModel(loadedControlNetModel);
                    }
                }
                controlNetModel.IsLoaded = await _stableDiffusionService.LoadControlNetModelAsync(controlNetModel.ModelSet);
                _logger.LogEnd($"ControlNet model '{controlNetModel.Name}' Loaded.", elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured while loading ControlNet model '{Name}'\nException Message: {Message}", controlNetModel.Name, ex.Message);
            }

            controlNetModel.IsLoading = false;
        }


        /// <summary>
        /// Unloads a ControlNet model.
        /// </summary>
        /// <param name="controlNetModel">The control net model.</param>
        private async Task UnloadControlNetModel(ControlNetModelSetViewModel controlNetModel)
        {
            if (!_stableDiffusionService.IsControlNetModelLoaded(controlNetModel.ModelSet))
                return;

            _logger.LogInformation("ControlNet model '{controlNetModel.Name}' Unloading...", controlNetModel.Name);
            controlNetModel.IsLoading = true;
            await _stableDiffusionService.UnloadControlNetModelAsync(controlNetModel.ModelSet);
            controlNetModel.IsLoading = false;
            controlNetModel.IsLoaded = false;
            _logger.LogInformation("ControlNet model '{controlNetModel.Name}' Unloaded.", controlNetModel.Name);
        }


        /// <summary>
        /// Called when Settings has changed.
        /// </summary>
        private void OnSettingsChanged()
        {
            // Base Models
            ModelCollectionView = new ListCollectionView(Settings.StableDiffusionModelSets);
            ModelCollectionView.Filter = (obj) =>
            {
                if (obj is not StableDiffusionModelSetViewModel viewModel)
                    return false;

                return viewModel.ModelSet.Diffusers.Intersect(SupportedDiffusers).Any();
            };

            //ControlNet models
            ControlNetModelCollectionView = new ListCollectionView(Settings.ControlNetModelSets);
            ControlNetModelCollectionView.Filter = (obj) =>
            {
                if (obj is not ControlNetModelSetViewModel viewModel)
                    return false;

                if (SelectedModel is null)
                    return false;

                if (!SelectedModel.IsControlNet)
                    return false;

                return viewModel.ModelSet.PipelineType == SelectedModel.ModelSet.PipelineType;
            };
        }


        /// <summary>
        /// Called when SupportedDiffusers has changed.
        /// </summary>
        private void OnSelectedModelChanged()
        {
            ControlNetModelCollectionView?.Refresh();
            SelectedControlNetModel = Settings.ControlNetModelSets.FirstOrDefault(x => x.IsLoaded && x.ModelSet.PipelineType == SelectedModel.ModelSet.PipelineType);
        }


        /// <summary>
        /// Called when SelectedModel has changed.
        /// </summary>
        private void OnSupportedDiffusersChanged()
        {
            ModelCollectionView?.Refresh();
        }


        /// <summary>
        /// Settings PropertyChangedCallback
        /// </summary>
        /// <returns></returns>
        private static PropertyChangedCallback OnSettingsChangedCalback()
        {
            return (d, e) =>
            {
                if (d is ModelPickerControl control && e.NewValue is AmuseSettings settings)
                    control.OnSettingsChanged();
            };
        }


        /// <summary>
        /// SupportedDiffusers PropertyChangedCallback
        /// </summary>
        /// <returns></returns>
        private static PropertyChangedCallback OnSupportedDiffusersChangedCallback()
        {
            return (d, e) =>
            {
                if (d is ModelPickerControl control && e.NewValue is List<DiffuserType> diffusers)
                    control.OnSupportedDiffusersChanged();
            };
        }


        /// <summary>
        /// SelectedModel PropertyChangedCallback
        /// </summary>
        /// <returns></returns>
        private static PropertyChangedCallback OnSelectedModelChangedCallback()
        {
            return (d, e) =>
            {
                if (d is ModelPickerControl control)
                    control.OnSelectedModelChanged();
            };
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }
}
