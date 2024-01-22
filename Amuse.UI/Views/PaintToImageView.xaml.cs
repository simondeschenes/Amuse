using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using Models;
using OnnxStack.Core.Image;
using OnnxStack.StableDiffusion.Common;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using OnnxStack.StableDiffusion.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Amuse.UI.Views
{
    /// <summary>
    /// Interaction logic for PaintToImageView.xaml
    /// </summary>
    public partial class PaintToImageView : UserControl, INavigatable, INotifyPropertyChanged
    {
        private readonly ILogger<PaintToImageView> _logger;
        private readonly IFileService _fileService;
        private readonly IStableDiffusionService _stableDiffusionService;
        private readonly IControlNetImageService _controlNetImageService;

        private bool _hasResult;
        private int _progressMax;
        private int _progressValue;
        private bool _isGenerating;
        private int _selectedTabIndex;
        private bool _hasInputResult;
        private bool _isControlsEnabled;
        private ImageInput _inputImage;
        private ImageInput _canvasImage;
        private ImageResult _resultImage;
        private StableDiffusionModelSetViewModel _selectedModel;
        private ControlNetModelSetViewModel _selectedControlNetModel;
        private PromptOptionsModel _promptOptionsModel;
        private SchedulerOptionsModel _schedulerOptions;
        private BatchOptionsModel _batchOptions;
        private CancellationTokenSource _cancelationTokenSource;
        private string _progressText;


        /// <summary>
        /// Initializes a new instance of the <see cref="PaintToImageView"/> class.
        /// </summary>
        public PaintToImageView()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _logger = App.GetService<ILogger<PaintToImageView>>();
                _fileService = App.GetService<IFileService>();
                _stableDiffusionService = App.GetService<IStableDiffusionService>();
                _controlNetImageService = App.GetService<IControlNetImageService>();
            }

            SupportedDiffusers = new() { DiffuserType.ImageToImage, DiffuserType.ControlNet, DiffuserType.ControlNetImage };
            CancelCommand = new AsyncRelayCommand(Cancel, CanExecuteCancel);
            GenerateCommand = new AsyncRelayCommand(Generate, CanExecuteGenerate);
            ClearHistoryCommand = new AsyncRelayCommand(ClearHistory, CanExecuteClearHistory);
            SaveImageCommand = new AsyncRelayCommand<ImageResult>(_fileService.SaveAsImageFile);
            SaveBlueprintCommand = new AsyncRelayCommand<ImageResult>(_fileService.SaveAsBlueprintFile);
            RemoveImageCommand = new AsyncRelayCommand<ImageResult>(RemoveImage);
            PromptOptions = new PromptOptionsModel();
            SchedulerOptions = new SchedulerOptionsModel { SchedulerType = SchedulerType.DDPM };
            BatchOptions = new BatchOptionsModel();
            ImageResults = new ObservableCollection<ImageResult>();
            ProgressMax = SchedulerOptions.InferenceSteps;
            IsControlsEnabled = true;
            HasCanvasChanged = true;
            InitializeComponent();
        }

        public AmuseSettings UISettings
        {
            get { return (AmuseSettings)GetValue(UISettingsProperty); }
            set { SetValue(UISettingsProperty, value); }
        }
        public static readonly DependencyProperty UISettingsProperty =
            DependencyProperty.Register("UISettings", typeof(AmuseSettings), typeof(PaintToImageView));

        public List<DiffuserType> SupportedDiffusers { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand GenerateCommand { get; }
        public AsyncRelayCommand ClearHistoryCommand { get; set; }
        public AsyncRelayCommand<ImageResult> SaveImageCommand { get; set; }
        public AsyncRelayCommand<ImageResult> SaveBlueprintCommand { get; set; }
        public AsyncRelayCommand<ImageResult> RemoveImageCommand { get; set; }
        public ObservableCollection<ImageResult> ImageResults { get; }

        public StableDiffusionModelSetViewModel SelectedModel
        {
            get { return _selectedModel; }
            set { _selectedModel = value; NotifyPropertyChanged(); }
        }

        public ControlNetModelSetViewModel SelectedControlNetModel
        {
            get { return _selectedControlNetModel; }
            set { _selectedControlNetModel = value; NotifyPropertyChanged(); }
        }

        public PromptOptionsModel PromptOptions
        {
            get { return _promptOptionsModel; }
            set { _promptOptionsModel = value; NotifyPropertyChanged(); }
        }

        public SchedulerOptionsModel SchedulerOptions
        {
            get { return _schedulerOptions; }
            set { _schedulerOptions = value; NotifyPropertyChanged(); }
        }

        public BatchOptionsModel BatchOptions
        {
            get { return _batchOptions; }
            set { _batchOptions = value; NotifyPropertyChanged(); }
        }

        public ImageResult ResultImage
        {
            get { return _resultImage; }
            set { _resultImage = value; NotifyPropertyChanged(); }
        }

        public ImageInput InputImage
        {
            get { return _inputImage; }
            set { _inputImage = value; NotifyPropertyChanged(); }
        }



        public ImageInput CanvasImage
        {
            get { return _canvasImage; }
            set { _canvasImage = value; NotifyPropertyChanged(); }
        }


        public int ProgressValue
        {
            get { return _progressValue; }
            set { _progressValue = value; NotifyPropertyChanged(); }
        }

        public int ProgressMax
        {
            get { return _progressMax; }
            set { _progressMax = value; NotifyPropertyChanged(); }
        }

        public string ProgressText
        {
            get { return _progressText; }
            set { _progressText = value; NotifyPropertyChanged(); }
        }

        public bool IsGenerating
        {
            get { return _isGenerating; }
            set { _isGenerating = value; NotifyPropertyChanged(); }
        }

        public bool HasResult
        {
            get { return _hasResult; }
            set { _hasResult = value; NotifyPropertyChanged(); }
        }

        public bool HasCanvasChanged
        {
            get { return _hasInputResult; }
            set { _hasInputResult = value; NotifyPropertyChanged(); }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set { _selectedTabIndex = value; NotifyPropertyChanged(); }
        }

        public bool IsControlsEnabled
        {
            get { return _isControlsEnabled; }
            set { _isControlsEnabled = value; NotifyPropertyChanged(); }
        }


        /// <summary>
        /// Called on Navigate
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <returns></returns>
        public async Task NavigateAsync(ImageResult imageResult)
        {
            if (IsGenerating)
                await Cancel();

            Reset();
            HasResult = false;
            ResultImage = null;
            CanvasImage = null;
            HasCanvasChanged = true;
            if (imageResult.Model.ModelSet.Diffusers.Contains(DiffuserType.ImageToImage))
            {
                SelectedModel = imageResult.Model;
            }
            InputImage = new ImageInput
            {
                Image = imageResult.Image,
                FileName = "Generated Image"
            };
            PromptOptions = PromptOptionsModel.FromPromptOptions(imageResult.PromptOptions);
            SchedulerOptions = SchedulerOptionsModel.FromSchedulerOptions(imageResult.SchedulerOptions);
            SelectedTabIndex = 0;
        }

        public Task NavigateAsync(VideoResultModel videoResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates this image result.
        /// </summary>
        private async Task Generate()
        {
            HasResult = false;
            IsGenerating = true;
            IsControlsEnabled = false;
            ResultImage = null;
            var promptOptions = await GetPromptOptionsAsync(PromptOptions, SchedulerOptions, CanvasImage);
            var batchOptions = BatchOptionsModel.ToBatchOptions(BatchOptions);
            var schedulerOptions = SchedulerOptionsModel.ToSchedulerOptions(SchedulerOptions);

            try
            {
                await foreach (var resultImage in ExecuteStableDiffusion(_selectedModel.ModelSet, _selectedControlNetModel?.ModelSet, promptOptions, schedulerOptions, batchOptions))
                {
                    if (resultImage != null)
                    {
                        ResultImage = resultImage;
                        HasResult = true;
                        if (BatchOptions.IsAutomationEnabled && BatchOptions.DisableHistory)
                            continue;
                        if (BatchOptions.IsRealtimeEnabled && !UISettings.RealtimeHistoryEnabled)
                            continue;

                        ImageResults.Add(resultImage);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Generate was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during Generate\n{ex}");
            }

            Reset();
        }


        /// <summary>
        /// Determines whether this instance can execute Generate.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can execute Generate; otherwise, <c>false</c>.
        /// </returns>
        private bool CanExecuteGenerate()
        {
            return !IsGenerating
                 && HasCanvasChanged
                 && SelectedModel is not null
                 && (!SelectedModel.IsControlNet || (SelectedModel.IsControlNet && SelectedControlNetModel?.IsLoaded == true));
        }


        /// <summary>
        /// Cancels this generation.
        /// </summary>
        /// <returns></returns>
        private Task Cancel()
        {
            _cancelationTokenSource?.Cancel();
            HasCanvasChanged = true;
            return Task.CompletedTask;
        }


        /// <summary>
        /// Determines whether this instance can execute Cancel.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can execute Cancel; otherwise, <c>false</c>.
        /// </returns>
        private bool CanExecuteCancel()
        {
            return IsGenerating;
        }


        /// <summary>
        /// Clears the history.
        /// </summary>
        /// <returns></returns>
        private Task ClearHistory()
        {
            ImageResults.Clear();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Determines whether this instance can execute ClearHistory.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can execute ClearHistory; otherwise, <c>false</c>.
        /// </returns>
        private bool CanExecuteClearHistory()
        {
            return ImageResults.Count > 0;
        }


        /// <summary>
        /// Removes the image.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private Task RemoveImage(ImageResult result)
        {
            ImageResults.Remove(result);
            if (result == ResultImage)
            {
                ResultImage = null;
                HasResult = false;
            }
            return Task.CompletedTask;
        }


        /// <summary>
        /// Resets this instance.
        /// </summary>
        private void Reset()
        {
            IsGenerating = false;
            IsControlsEnabled = true;
            ProgressValue = 0;
            Utils.TaskbarProgress(0, 0);
        }


        /// <summary>
        /// Executes the stable diffusion process.
        /// </summary>
        /// <param name="modelOptions">The model options.</param>
        /// <param name="promptOptions">The prompt options.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        /// <param name="batchOptions">The batch options.</param>
        /// <returns></returns>
        private async IAsyncEnumerable<ImageResult> ExecuteStableDiffusion(StableDiffusionModelSet modelOptions, ControlNetModelSet controlNetModel, PromptOptions promptOptions, SchedulerOptions schedulerOptions, BatchOptions batchOptions)
        {
            _cancelationTokenSource = new CancellationTokenSource();

            if (!BatchOptions.IsRealtimeEnabled)
            {
                if (!BatchOptions.IsAutomationEnabled)
                {
                    var timestamp = Stopwatch.GetTimestamp();
                    var result = await _stableDiffusionService.GenerateAsBytesAsync(new ModelOptions(modelOptions, controlNetModel), promptOptions, schedulerOptions, ProgressCallback(), _cancelationTokenSource.Token);
                    yield return await GenerateResultAsync(result, promptOptions, schedulerOptions, timestamp);
                }
                else
                {
                    if (!BatchOptions.IsRealtimeEnabled)
                    {
                        var timestamp = Stopwatch.GetTimestamp();
                        await foreach (var batchResult in _stableDiffusionService.GenerateBatchAsync(new ModelOptions(modelOptions, controlNetModel), promptOptions, schedulerOptions, batchOptions, ProgressBatchCallback(), _cancelationTokenSource.Token))
                        {
                            yield return await GenerateResultAsync(batchResult.ImageResult.ToImageBytes(), promptOptions, batchResult.SchedulerOptions, timestamp);
                            timestamp = Stopwatch.GetTimestamp();
                        }
                    }
                }
            }
            else
            {
                // Realtime Diffusion
                IsControlsEnabled = true;
                SchedulerOptions.Seed = SchedulerOptions.Seed == 0 ? Random.Shared.Next() : SchedulerOptions.Seed;
                while (!_cancelationTokenSource.IsCancellationRequested)
                {
                    var refreshTimestamp = Stopwatch.GetTimestamp();
                    if (SchedulerOptions.HasChanged || PromptOptions.HasChanged || HasCanvasChanged || SchedulerOptions.Seed == 0)
                    {
                        HasCanvasChanged = false;
                        PromptOptions.HasChanged = false;
                        SchedulerOptions.HasChanged = false;
                        var realtimePromptOptions = await GetPromptOptionsAsync(PromptOptions, SchedulerOptions, CanvasImage);
                        var realtimeSchedulerOptions = SchedulerOptionsModel.ToSchedulerOptions(SchedulerOptions);

                        var timestamp = Stopwatch.GetTimestamp();
                        var result = await _stableDiffusionService.GenerateAsBytesAsync(new ModelOptions(modelOptions, controlNetModel), realtimePromptOptions, realtimeSchedulerOptions, RealtimeProgressCallback(), _cancelationTokenSource.Token);
                        yield return await GenerateResultAsync(result, realtimePromptOptions, realtimeSchedulerOptions, timestamp);
                    }
                    await Utils.RefreshDelay(refreshTimestamp, UISettings.RealtimeRefreshRate, _cancelationTokenSource.Token);
                }
            }
        }


        private async Task<PromptOptions> GetPromptOptionsAsync(PromptOptionsModel promptOptionsModel, SchedulerOptionsModel schedulerOptionsModel, ImageInput imageInput)
        {
            var imageBytes = imageInput.Image.GetImageBytes();
            if (_selectedModel.IsControlNet)
            {
                var controlNetDiffuserType = _schedulerOptions.Strength >= 1
                        ? DiffuserType.ControlNet
                        : DiffuserType.ControlNetImage;

                var inputImage = default(InputImage);
                if (controlNetDiffuserType == DiffuserType.ControlNetImage)
                    inputImage = new InputImage(imageBytes);

                var controlImage = new InputImage(imageBytes);
                if (_selectedControlNetModel.HasAnnotator && schedulerOptionsModel.IsControlImageProcessingEnabled)
                    controlImage = await _controlNetImageService.PrepareInputImage(_selectedControlNetModel.ModelSet, controlImage, _schedulerOptions.Height, _schedulerOptions.Width);

                return new PromptOptions
                {
                    Prompt = promptOptionsModel.Prompt,
                    NegativePrompt = promptOptionsModel.NegativePrompt,
                    DiffuserType = controlNetDiffuserType,
                    InputImage = inputImage,
                    InputContolImage = controlImage
                };
            }

            return new PromptOptions
            {
                Prompt = promptOptionsModel.Prompt,
                NegativePrompt = promptOptionsModel.NegativePrompt,
                DiffuserType = DiffuserType.ImageToImage,
                InputImage = new InputImage(imageBytes)
            };
        }


        /// <summary>
        /// Generates the result.
        /// </summary>
        /// <param name="imageBytes">The image bytes.</param>
        /// <param name="promptOptions">The prompt options.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        private async Task<ImageResult> GenerateResultAsync(byte[] imageBytes, PromptOptions promptOptions, SchedulerOptions schedulerOptions, long timestamp)
        {
            var image = Utils.CreateBitmap(imageBytes);

            var imageResult = new ImageResult
            {
                Image = image,
                Model = _selectedModel,
                PromptOptions = promptOptions,
                PipelineType = _selectedModel.ModelSet.PipelineType,
                DiffuserType = promptOptions.DiffuserType,
                SchedulerOptions = schedulerOptions,
                Elapsed = Stopwatch.GetElapsedTime(timestamp).TotalSeconds
            };

            await _fileService.AutoSaveImageFile(imageResult, "PaintToImage");
            return imageResult;
        }


        /// <summary>
        /// StableDiffusion progress callback.
        /// </summary>
        /// <returns></returns>
        private Action<DiffusionProgress> ProgressCallback()
        {
            return (progress) =>
            {
                App.UIInvoke(() =>
                {
                    if (_cancelationTokenSource.IsCancellationRequested)
                        return;

                    if (ProgressMax != progress.StepMax)
                        ProgressMax = progress.StepMax;

                    ProgressValue = progress.StepValue;
                    ProgressText = $"Step: {progress.StepValue:D2}/{progress.StepMax:D2}";
                });
                Utils.TaskbarProgress(progress.StepValue, progress.StepMax);
            };
        }

        private Action<DiffusionProgress> ProgressBatchCallback()
        {
            return (progress) =>
            {
                App.UIInvoke(() =>
                {
                    if (_cancelationTokenSource.IsCancellationRequested)
                        return;

                    if (BatchOptions.BatchsValue != progress.BatchMax)
                        BatchOptions.BatchsValue = progress.BatchMax;
                    if (BatchOptions.BatchValue != progress.BatchValue)
                        BatchOptions.BatchValue = progress.BatchValue;
                    if (BatchOptions.StepValue != progress.StepValue)
                        BatchOptions.StepValue = progress.StepValue;
                    if (BatchOptions.StepsValue != progress.StepMax)
                        BatchOptions.StepsValue = progress.StepMax;
                });
                Utils.TaskbarProgress(progress.BatchValue, progress.BatchMax);
            };
        }

        private Action<DiffusionProgress> RealtimeProgressCallback()
        {
            return (progress) =>
            {
                App.UIInvoke(() =>
                {
                    if (_cancelationTokenSource.IsCancellationRequested)
                        return;

                    if (BatchOptions.StepValue != progress.StepValue)
                        BatchOptions.StepValue = progress.StepValue;
                    if (BatchOptions.StepsValue != progress.StepMax)
                        BatchOptions.StepsValue = progress.StepMax;
                });
                Utils.TaskbarProgress(progress.StepValue, progress.StepMax);
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