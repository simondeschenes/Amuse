using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using Models;
using OnnxStack.Core.Image;
using OnnxStack.Core.Video;
using OnnxStack.StableDiffusion.Common;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
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
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Amuse.UI.Views
{
    /// <summary>
    /// Interaction logic for VideoToVideoView.xaml
    /// </summary>
    public partial class VideoToVideoView : UserControl, INavigatable, INotifyPropertyChanged
    {
        private readonly ILogger<VideoToVideoView> _logger;
        private readonly IStableDiffusionService _stableDiffusionService;
        private readonly IFileService _fileService;

        private bool _hasResult;
        private int _progressMax;
        private int _progressValue;
        private bool _isGenerating;
        private int _selectedTabIndex;
        private bool _hasInputResult;
        private bool _isControlsEnabled;
        private VideoInputModel _inputVideo;
        private VideoResultModel _resultVideo;
        private StableDiffusionModelSetViewModel _selectedModel;
        private ControlNetModelSetViewModel _selectedControlNetModel;
        private PromptOptionsModel _promptOptionsModel;
        private SchedulerOptionsModel _schedulerOptions;
        private BatchOptionsModel _batchOptions;
        private CancellationTokenSource _cancelationTokenSource;
        private OnnxVideo _videoFrames;
        private BitmapImage _previewSource;
        private BitmapImage _previewResult;
        private string _progressText;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoToVideoView"/> class.
        /// </summary>
        public VideoToVideoView()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _logger = App.GetService<ILogger<VideoToVideoView>>();
                _fileService = App.GetService<IFileService>();
                _stableDiffusionService = App.GetService<IStableDiffusionService>();
            }

            SupportedDiffusers = new() { DiffuserType.ImageToImage, DiffuserType.ControlNet, DiffuserType.ControlNetImage };
            CancelCommand = new AsyncRelayCommand(Cancel, CanExecuteCancel);
            GenerateCommand = new AsyncRelayCommand(Generate, CanExecuteGenerate);
            ClearHistoryCommand = new AsyncRelayCommand(ClearHistory, CanExecuteClearHistory);
            NavigateCommand = new AsyncRelayCommand<VideoResultModel>(NavigateAsync);
            SaveVideoCommand = new AsyncRelayCommand<VideoResultModel>(_fileService.SaveAsVideoFile);
            SaveBlueprintCommand = new AsyncRelayCommand<VideoResultModel>(_fileService.SaveAsBlueprintFile);
            RemoveVideoCommand = new AsyncRelayCommand<VideoResultModel>(RemoveVideo);
            PromptOptions = new PromptOptionsModel();
            SchedulerOptions = new SchedulerOptionsModel();
            BatchOptions = new BatchOptionsModel();
            VideoResults = new ObservableCollection<VideoResultModel>();
            ProgressMax = SchedulerOptions.InferenceSteps;
            IsControlsEnabled = true;
            InitializeComponent();
        }

        public AmuseSettings UISettings
        {
            get { return (AmuseSettings)GetValue(UISettingsProperty); }
            set { SetValue(UISettingsProperty, value); }
        }
        public static readonly DependencyProperty UISettingsProperty =
            DependencyProperty.Register("UISettings", typeof(AmuseSettings), typeof(VideoToVideoView));

        public INavigatable SelectedTab
        {
            get { return (INavigatable)GetValue(SelectedTabProperty); }
            set { SetValue(SelectedTabProperty, value); }
        }
        public static readonly DependencyProperty SelectedTabProperty =
            DependencyProperty.Register("SelectedTab", typeof(INavigatable), typeof(VideoToVideoView));

        public List<DiffuserType> SupportedDiffusers { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand GenerateCommand { get; }
        public AsyncRelayCommand ClearHistoryCommand { get; }
        public AsyncRelayCommand<VideoResultModel> SaveVideoCommand { get; }
        public AsyncRelayCommand<VideoResultModel> SaveBlueprintCommand { get; set; }
        public AsyncRelayCommand<VideoResultModel> RemoveVideoCommand { get; set; }

        public ObservableCollection<VideoResultModel> VideoResults { get; }

        public AsyncRelayCommand<VideoResultModel> NavigateCommand { get; set; }

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

        public VideoResultModel ResultVideo
        {
            get { return _resultVideo; }
            set { _resultVideo = value; NotifyPropertyChanged(); }
        }

        public VideoInputModel InputVideo
        {
            get { return _inputVideo; }
            set { _inputVideo = value; _videoFrames = null; NotifyPropertyChanged(); }
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

        public bool HasInputResult
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

        public BitmapImage PreviewSource
        {
            get { return _previewSource; }
            set { _previewSource = value; NotifyPropertyChanged(); }
        }

        public BitmapImage PreviewResult
        {
            get { return _previewResult; }
            set { _previewResult = value; NotifyPropertyChanged(); }
        }


        /// <summary>
        /// Called on Navigate
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <returns></returns>
        public Task NavigateAsync(ImageResult imageResult)
        {
            throw new NotImplementedException();
        }


        public async Task NavigateAsync(VideoResultModel videoResult)
        {
            SelectedTab = this;
            if (IsGenerating)
                await Cancel();

            Reset();
            HasResult = false;
            ResultVideo = null;

            if (videoResult.Model.ModelSet.Diffusers.Contains(DiffuserType.ImageToImage))
            {
                SelectedModel = videoResult.Model;
            }
            InputVideo = new VideoInputModel
            {
                FileName = videoResult.FileName,
                Video = videoResult.Video,
            };
            PromptOptions = PromptOptionsModel.FromPromptOptions(videoResult.PromptOptions);
            SchedulerOptions = SchedulerOptionsModel.FromSchedulerOptions(videoResult.SchedulerOptions);
            SelectedTabIndex = 0;
        }


        /// <summary>
        /// Generates this image result.
        /// </summary>
        private async Task Generate()
        {
            HasResult = false;
            IsGenerating = true;
            IsControlsEnabled = false;
            ResultVideo = null;
            ProgressValue = 0;
            _cancelationTokenSource = new CancellationTokenSource();

            try
            {
                var batchOptions = BatchOptionsModel.ToBatchOptions(BatchOptions);
                var schedulerOptions = SchedulerOptionsModel.ToSchedulerOptions(SchedulerOptions);
                if (_videoFrames is null)// || _videoFrames.Info.FrameRate != PromptOptions.VideoInputFPS)
                {
                    ProgressText = $"Generating video frames @ {_inputVideo.Video.FrameRate}fps";
                    _videoFrames = new OnnxVideo(_inputVideo.Video.Info, await VideoHelper.ReadVideoFramesAsync(_inputVideo.FileName, _inputVideo.Video.FrameRate, _cancelationTokenSource.Token));
                }

                var promptOptions = GetPromptOptions(PromptOptions, _videoFrames);
                await foreach (var resultVideo in ExecuteStableDiffusion(_selectedModel.ModelSet, _selectedControlNetModel?.ModelSet, promptOptions, schedulerOptions, batchOptions))
                {
                    if (resultVideo != null)
                    {
                        ResultVideo = resultVideo;
                        VideoResults.Add(resultVideo);
                        if (BatchOptions.IsAutomationEnabled && BatchOptions.DisableHistory)
                            continue;
                        if (BatchOptions.IsRealtimeEnabled && !UISettings.RealtimeHistoryEnabled)
                            continue;

                        HasResult = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                HasResult = ResultVideo is not null;
                _logger.LogInformation($"Generate was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during Generate\n{ex.Message}");
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
                && HasInputResult
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
        private async Task ClearHistory()
        {
            await _fileService.DeleteTempVideoFile(VideoResults);
            VideoResults.Clear();
            ResultVideo = null;
            HasResult = false;
        }


        /// <summary>
        /// Determines whether this instance can execute ClearHistory.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can execute ClearHistory; otherwise, <c>false</c>.
        /// </returns>
        private bool CanExecuteClearHistory()
        {
            return VideoResults.Count > 0;
        }


        /// <summary>
        /// Removes the video.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task RemoveVideo(VideoResultModel result)
        {
            await _fileService.DeleteTempVideoFile(result);
            VideoResults.Remove(result);
            if (result == ResultVideo)
            {
                ResultVideo = null;
                HasResult = false;
            }
        }


        /// <summary>
        /// Resets this instance.
        /// </summary>
        private void Reset()
        {
            PreviewSource = null;
            PreviewResult = null;
            IsGenerating = false;
            IsControlsEnabled = true;
            ProgressValue = 0;
            ProgressMax = 1;
            ProgressText = null;
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
        private async IAsyncEnumerable<VideoResultModel> ExecuteStableDiffusion(StableDiffusionModelSet modelOptions, ControlNetModelSet controlNetModel, PromptOptions promptOptions, SchedulerOptions schedulerOptions, BatchOptions batchOptions)
        {
            if (!BatchOptions.IsRealtimeEnabled)
            {
                if (!BatchOptions.IsAutomationEnabled)
                {
                    var timestamp = Stopwatch.GetTimestamp();
                    var result = await _stableDiffusionService.GenerateVideoAsync(new ModelOptions(modelOptions, controlNetModel), promptOptions, schedulerOptions, ProgressCallback(), _cancelationTokenSource.Token);
                    yield return await GenerateResultAsync(result, _selectedModel, promptOptions, schedulerOptions, timestamp);
                }
                else
                {
                    if (!BatchOptions.IsRealtimeEnabled)
                    {
                        var timestamp = Stopwatch.GetTimestamp();
                        await foreach (var batchResult in _stableDiffusionService.GenerateVideoAsync(batchOptions, new ModelOptions(modelOptions), promptOptions, schedulerOptions, ProgressCallback(), _cancelationTokenSource.Token))
                        {
                            yield return await GenerateResultAsync(new OnnxVideo(_videoFrames.Info, batchResult.Result), _selectedModel, promptOptions, batchResult.SchedulerOptions, timestamp);
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
                    ProgressValue = 0;
                    IsControlsEnabled = true;
                    ProgressMax = _videoFrames.Frames.Count;
                    var timestamp = Stopwatch.GetTimestamp();
                    var resultVideoFrames = new List<OnnxImage>();
                    foreach (var videoFrame in _videoFrames.Frames)
                    {
                        var refreshTimestamp = Stopwatch.GetTimestamp();
                        var realtimePromptOptions = GetLivePromptOptions(PromptOptions, videoFrame);
                        var realtimeSchedulerOptions = SchedulerOptionsModel.ToSchedulerOptions(SchedulerOptions);
                        var result = await _stableDiffusionService.GenerateImageAsync(new ModelOptions(modelOptions, controlNetModel), realtimePromptOptions, realtimeSchedulerOptions, RealtimeProgressCallback(), _cancelationTokenSource.Token);
                        resultVideoFrames.Add(result);
                        PreviewResult = await result.ToBitmapAsync();
                        PreviewSource = await videoFrame.ToBitmapAsync();
                        ProgressValue++;
                        await Utils.RefreshDelay(refreshTimestamp, UISettings.RealtimeRefreshRate, _cancelationTokenSource.Token);
                    }

                    IsControlsEnabled = false;
                    var videoResult = new OnnxVideo(_videoFrames.Info, resultVideoFrames);
                    yield return await GenerateResultAsync(videoResult, _selectedModel, GetLivePromptOptions(PromptOptions, default), SchedulerOptionsModel.ToSchedulerOptions(SchedulerOptions), timestamp);
                }
            }
        }


        private PromptOptions GetPromptOptions(PromptOptionsModel promptOptionsModel, OnnxVideo videoFrames)
        {
            var diffuserType = DiffuserType.ImageToImage;
            if (_selectedModel.IsControlNet)
            {
                diffuserType = _schedulerOptions.Strength >= 1
                        ? DiffuserType.ControlNet
                        : DiffuserType.ControlNetImage;
            }

            return new PromptOptions
            {
                Prompt = string.IsNullOrEmpty(promptOptionsModel.Prompt) ? " " : promptOptionsModel.Prompt,
                NegativePrompt = promptOptionsModel.NegativePrompt,
                DiffuserType = diffuserType,
                InputVideo = videoFrames
            };
        }


        private PromptOptions GetLivePromptOptions(PromptOptionsModel promptOptionsModel, OnnxImage videoFrame)
        {
            return new PromptOptions
            {
                Prompt = string.IsNullOrEmpty(promptOptionsModel.Prompt) ? " " : promptOptionsModel.Prompt,
                NegativePrompt = promptOptionsModel.NegativePrompt,
                DiffuserType = DiffuserType.ImageToImage,
                InputImage = videoFrame
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
        private async Task<VideoResultModel> GenerateResultAsync(OnnxVideo video, StableDiffusionModelSetViewModel modelOptions, PromptOptions promptOptions, SchedulerOptions schedulerOptions, long timestamp)
        {
            var tempvideoFileName = await _fileService.SaveTempVideoFile(video, "VideoToVideo");
            var videoResult = new VideoResultModel
            {
                Video = video,
                Model = modelOptions,
                FileName = tempvideoFileName,
                PromptOptions = promptOptions,
                SchedulerOptions = schedulerOptions,
                DiffuserType = promptOptions.DiffuserType,
                PipelineType = _selectedModel.ModelSet.PipelineType,
                Elapsed = Stopwatch.GetElapsedTime(timestamp).TotalSeconds
            };

            await _fileService.AutoSaveVideoFile(videoResult, "VideoToVideo");
            return videoResult;
        }



        /// <summary>
        /// StableDiffusion progress callback.
        /// </summary>
        /// <returns></returns>
        private Action<DiffusionProgress> ProgressCallback()
        {
            return (progress) =>
            {
                if (_cancelationTokenSource.IsCancellationRequested)
                    return;

                App.UIInvoke(async () =>
                {
                    if (_cancelationTokenSource.IsCancellationRequested)
                        return;

                    if (progress.BatchTensor is null)
                        ProgressText = $"Frame: {progress.BatchValue:D2}/{_videoFrames.Frames.Count}  |  Step: {progress.StepValue:D2}/{progress.StepMax:D2}";


                    if (progress.BatchTensor is not null)
                    {
                        PreviewResult = await new OnnxImage(progress.BatchTensor).ToBitmapAsync();
                        PreviewSource = await _videoFrames.Frames[progress.BatchValue - 1].ToBitmapAsync();
                    }

                    if (ProgressText != progress.Message && progress.BatchMax == 0)
                        ProgressText = progress.Message;

                    if (ProgressMax != progress.BatchMax)
                        ProgressMax = progress.BatchMax;

                    if (ProgressValue != progress.BatchValue)
                        ProgressValue = progress.BatchValue;

                }, DispatcherPriority.Background);

                if (progress.BatchTensor is null)
                    Utils.TaskbarProgress((progress.StepMax * (progress.BatchValue - 1)) + progress.StepValue, _videoFrames.Frames.Count * progress.StepMax);
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

                    ProgressText = $"Frame: {(ProgressValue):D2}/{ProgressMax}  |  Step: {progress.StepValue:D2}/{progress.StepMax:D2}";
                    if (progress.BatchTensor is null)
                        Utils.TaskbarProgress((progress.StepMax * ProgressValue) + progress.StepValue, ProgressMax * progress.StepMax);

                });
                Utils.TaskbarProgress(ProgressValue, ProgressMax);
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