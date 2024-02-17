using Amuse.UI.Commands;
using Amuse.UI.Models;
using Models;
using OnnxStack.Core;
using OnnxStack.StableDiffusion;
using OnnxStack.StableDiffusion.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amuse.UI.UserControls
{
    /// <summary>
    /// Interaction logic for Parameters.xaml
    /// </summary>
    public partial class SchedulerControl : UserControl, INotifyPropertyChanged
    {
        private StableDiffusionSchedulerDefaults _schedulerDefaults;
        private List<SchedulerType> _schedulerTypes = new();

        /// <summary>Initializes a new instance of the <see cref="SchedulerControl" /> class.</summary>
        public SchedulerControl()
        {
            ValidSizes = new List<int>(new[] { 64, 128, 192, 256, 320, 384, 448, 512, 576, 640, 704, 768, 832, 896, 960, 1024 });
            NewSeedCommand = new RelayCommand(NewSeed);
            RandomSeedCommand = new RelayCommand(RandomSeed);
            ResetParametersCommand = new RelayCommand(ResetParameters);
            InitializeComponent();
        }

        public ICommand ResetParametersCommand { get; }
        public ICommand NewSeedCommand { get; }
        public ICommand RandomSeedCommand { get; }
        public List<int> ValidSizes { get; }

        public AmuseSettings UISettings
        {
            get { return (AmuseSettings)GetValue(UISettingsProperty); }
            set { SetValue(UISettingsProperty, value); }
        }
        public static readonly DependencyProperty UISettingsProperty =
            DependencyProperty.Register("UISettings", typeof(AmuseSettings), typeof(SchedulerControl));


        public List<SchedulerType> SchedulerTypes
        {
            get { return _schedulerTypes; }
            set { _schedulerTypes = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the selected model.
        /// </summary>
        public StableDiffusionModelSetViewModel SelectedModel
        {
            get { return (StableDiffusionModelSetViewModel)GetValue(SelectedModelProperty); }
            set { SetValue(SelectedModelProperty, value); }
        }
        public static readonly DependencyProperty SelectedModelProperty =
            DependencyProperty.Register("SelectedModel", typeof(StableDiffusionModelSetViewModel), typeof(SchedulerControl), new PropertyMetadata((d, e) =>
            {
                if (d is SchedulerControl schedulerControl)
                    schedulerControl.OnModelChanged(e.NewValue as StableDiffusionModelSetViewModel);
            }));




        public ControlNetModelSetViewModel SelectedControlNetModel
        {
            get { return (ControlNetModelSetViewModel)GetValue(SelectedControlNetModelProperty); }
            set { SetValue(SelectedControlNetModelProperty, value); }
        }

        public static readonly DependencyProperty SelectedControlNetModelProperty =
            DependencyProperty.Register("SelectedControlNetModel", typeof(ControlNetModelSetViewModel), typeof(SchedulerControl));




        /// <summary>
        /// Gets or sets the type of the diffuser.
        /// </summary>
        public DiffuserType DiffuserType
        {
            get { return (DiffuserType)GetValue(DiffuserTypeProperty); }
            set { SetValue(DiffuserTypeProperty, value); }
        }
        public static readonly DependencyProperty DiffuserTypeProperty =
            DependencyProperty.Register("DiffuserType", typeof(DiffuserType), typeof(SchedulerControl));


        /// <summary>
        /// Gets or sets the SchedulerOptions.
        /// </summary>
        public SchedulerOptionsModel SchedulerOptions
        {
            get { return (SchedulerOptionsModel)GetValue(SchedulerOptionsProperty); }
            set { SetValue(SchedulerOptionsProperty, value); }
        }
        public static readonly DependencyProperty SchedulerOptionsProperty =
            DependencyProperty.Register("SchedulerOptions", typeof(SchedulerOptionsModel), typeof(SchedulerControl));


        public BatchOptionsModel BatchOptions
        {
            get { return (BatchOptionsModel)GetValue(BatchOptionsProperty); }
            set { SetValue(BatchOptionsProperty, value); }
        }
        public static readonly DependencyProperty BatchOptionsProperty =
            DependencyProperty.Register("BatchOptions", typeof(BatchOptionsModel), typeof(SchedulerControl));


        public bool IsGenerating
        {
            get { return (bool)GetValue(IsGeneratingProperty); }
            set { SetValue(IsGeneratingProperty, value); }
        }
        public static readonly DependencyProperty IsGeneratingProperty =
            DependencyProperty.Register("IsGenerating", typeof(bool), typeof(SchedulerControl));


        public bool IsVideoControlsEnabled
        {
            get { return (bool)GetValue(IsVideoControlsEnabledProperty); }
            set { SetValue(IsVideoControlsEnabledProperty, value); }
        }
        public static readonly DependencyProperty IsVideoControlsEnabledProperty =
            DependencyProperty.Register("IsVideoControlsEnabled", typeof(bool), typeof(SchedulerControl));



        public StableDiffusionSchedulerDefaults SchedulerDefaults
        {
            get { return _schedulerDefaults; }
            set { _schedulerDefaults = value; NotifyPropertyChanged(); }
        }


        /// <summary>
        /// Called when the selected model has changed.
        /// </summary>
        /// <param name="modelOptionsModel">The model options model.</param>
        private void OnModelChanged(StableDiffusionModelSetViewModel model)
        {
            if (model is null)
                return;

            SchedulerTypes = new List<SchedulerType>(GetSchedulerTypes(model.ModelSet.PipelineType));
            SchedulerDefaults = UISettings.Templates.FirstOrDefault(x => x.Name == model.Name)?.StableDiffusionTemplate?.SchedulerDefaults
                    ?? new StableDiffusionSchedulerDefaults();
            ResetParameters();
        }


        /// <summary>
        /// Resets the parameters.
        /// </summary>
        private void ResetParameters()
        {
            SchedulerOptions = new SchedulerOptionsModel
            {
                SchedulerType = SchedulerDefaults.SchedulerType,
                GuidanceScale = SchedulerDefaults.Guidance,
                InferenceSteps = SchedulerDefaults.Steps,
                Width = SelectedModel.ModelSet.SampleSize,
                Height = SelectedModel.ModelSet.SampleSize,
                Strength = SelectedModel.IsControlNet ? 1 : 0.75f,
            };
        }

        private void NewSeed()
        {
            SchedulerOptions.Seed = Random.Shared.Next();
        }

        private void RandomSeed()
        {
            SchedulerOptions.Seed = 0;
        }

        /// <summary>
        /// TODO: Get from pipeline
        /// </summary>
        /// <param name="pipelineType">Type of the pipeline.</param>
        /// <returns></returns>
        private static SchedulerType[] GetSchedulerTypes(DiffuserPipelineType pipelineType)
        {
            switch (pipelineType)
            {
                case DiffuserPipelineType.StableDiffusion:
                case DiffuserPipelineType.StableDiffusionXL:
                    return new[]
                    {
                        SchedulerType.LMS,
                        SchedulerType.Euler,
                        SchedulerType.EulerAncestral,
                        SchedulerType.DDPM,
                        SchedulerType.DDIM,
                        SchedulerType.KDPM2
                    };
                case DiffuserPipelineType.LatentConsistency:
                case DiffuserPipelineType.LatentConsistencyXL:
                    return new[]
                    {
                        SchedulerType.LCM
                    };
                case DiffuserPipelineType.InstaFlow:
                    return new[]
                    {
                        SchedulerType.InstaFlow
                    };
                default:
                    return default;
            }
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
