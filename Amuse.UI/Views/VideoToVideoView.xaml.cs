using Amuse.UI.Commands;
using Amuse.UI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Models;
using OnnxStack.Core.Image;
using OnnxStack.Core.Services;
using OnnxStack.Core.Video;
using OnnxStack.StableDiffusion.Common;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using OnnxStack.StableDiffusion.Helpers;
using OnnxStack.StableDiffusion.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        private readonly IVideoService _videoService;

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
        private PromptOptionsModel _promptOptionsModel;
        private SchedulerOptionsModel _schedulerOptions;
        private BatchOptionsModel _batchOptions;
        private CancellationTokenSource _cancelationTokenSource;
        private VideoFrames _videoFrames;
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
                _videoService = App.GetService<IVideoService>();
                _stableDiffusionService = App.GetService<IStableDiffusionService>();
            }

            SupportedDiffusers = new() { DiffuserType.ImageToImage };
            CancelCommand = new AsyncRelayCommand(Cancel, CanExecuteCancel);
            GenerateCommand = new AsyncRelayCommand(Generate, CanExecuteGenerate);
            ClearHistoryCommand = new AsyncRelayCommand(ClearHistory, CanExecuteClearHistory);
            NavigateCommand = new AsyncRelayCommand<VideoResultModel>(NavigateAsync);
            SaveVideoCommand = new AsyncRelayCommand<VideoResultModel>(SaveVideoFile);
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

        public ObservableCollection<VideoResultModel> VideoResults { get; }

        public AsyncRelayCommand<VideoResultModel> NavigateCommand { get; set; }

        public StableDiffusionModelSetViewModel SelectedModel
        {
            get { return _selectedModel; }
            set { _selectedModel = value; NotifyPropertyChanged(); }
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
                VideoBytes = videoResult.VideoBytes,
                VideoInfo = videoResult.VideoInfo,
            };
            PromptOptions = PromptOptionsModel.FromPromptOptions(videoResult.PromptOptions);
            SchedulerOptions = videoResult.SchedulerOptions.ToSchedulerOptionsModel();
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
                var batchOptions = BatchOptions.ToBatchOptions();
                var schedulerOptions = SchedulerOptions.ToSchedulerOptions();
                if (_videoFrames is null || _videoFrames.Info.FPS != PromptOptions.VideoInputFPS)
                {
                    ProgressText = $"Generating video frames @ {PromptOptions.VideoInputFPS}fps";
                    _videoFrames = await _videoService.CreateFramesAsync(_inputVideo.VideoBytes, PromptOptions.VideoInputFPS, _cancelationTokenSource.Token);
                }

                var promptOptions = GetPromptOptions(PromptOptions, _videoFrames);

                await foreach (var resultVideo in ExecuteStableDiffusion(_selectedModel.ModelSet, promptOptions, schedulerOptions, batchOptions))
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
                && HasInputResult;
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
        private Task ClearHistory()
        {
            try
            {
                foreach (var videoResult in VideoResults)
                {
                    if (videoResult.Equals(ResultVideo?.FileName))
                        continue;

                    Task.Run(() => File.Delete(videoResult.FileName));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ClearHistory] Failed to delete video files: {ex.Message}");
            }

            VideoResults.Clear();
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
            return VideoResults.Count > 0;
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
        }


        /// <summary>
        /// Executes the stable diffusion process.
        /// </summary>
        /// <param name="modelOptions">The model options.</param>
        /// <param name="promptOptions">The prompt options.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        /// <param name="batchOptions">The batch options.</param>
        /// <returns></returns>
        private async IAsyncEnumerable<VideoResultModel> ExecuteStableDiffusion(StableDiffusionModelSet modelOptions, PromptOptions promptOptions, SchedulerOptions schedulerOptions, BatchOptions batchOptions)
        {
            if (!BatchOptions.IsRealtimeEnabled)
            {
                if (!BatchOptions.IsAutomationEnabled)
                {
                    var timestamp = Stopwatch.GetTimestamp();
                    var result = await _stableDiffusionService.GenerateAsBytesAsync(new ModelOptions(modelOptions), promptOptions, schedulerOptions, ProgressCallback(), _cancelationTokenSource.Token);
                    yield return await GenerateResultAsync(result, _selectedModel, promptOptions, schedulerOptions, timestamp);
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
                    var resultVideoFrames = new List<byte[]>();
                    foreach (var videoFrame in _videoFrames.Frames)
                    {
                        var refreshTimestamp = Stopwatch.GetTimestamp();
                        var realtimePromptOptions = GetLivePromptOptions(PromptOptions, videoFrame);
                        var realtimeSchedulerOptions = SchedulerOptions.ToSchedulerOptions();
                        var result = await _stableDiffusionService.GenerateAsBytesAsync(new ModelOptions(modelOptions), realtimePromptOptions, realtimeSchedulerOptions, RealtimeProgressCallback(), _cancelationTokenSource.Token);
                        resultVideoFrames.Add(result);
                        PreviewResult = Utils.CreateBitmap(result);
                        PreviewSource = Utils.CreateBitmap(videoFrame);
                        ProgressValue++;
                        await Utils.RefreshDelay(refreshTimestamp, UISettings.RealtimeRefreshRate, _cancelationTokenSource.Token);
                    }

                    IsControlsEnabled = false;
                    var videoResult = await _videoService.CreateVideoAsync(resultVideoFrames, _promptOptionsModel.VideoOutputFPS);
                    yield return await GenerateResultAsync(videoResult.Data, _selectedModel, GetLivePromptOptions(PromptOptions, default), SchedulerOptions.ToSchedulerOptions(), timestamp);
                }
            }
        }


        private PromptOptions GetPromptOptions(PromptOptionsModel promptOptionsModel, VideoFrames videoFrames)
        {
            return new PromptOptions
            {
                Prompt = string.IsNullOrEmpty(promptOptionsModel.Prompt) ? " " : promptOptionsModel.Prompt,
                NegativePrompt = promptOptionsModel.NegativePrompt,
                DiffuserType = DiffuserType.ImageToImage,
                InputVideo = new VideoInput(videoFrames),
                VideoInputFPS = promptOptionsModel.VideoInputFPS,
                VideoOutputFPS = promptOptionsModel.VideoOutputFPS,
            };
        }


        private PromptOptions GetLivePromptOptions(PromptOptionsModel promptOptionsModel, byte[] videoFrame)
        {
            return new PromptOptions
            {
                Prompt = string.IsNullOrEmpty(promptOptionsModel.Prompt) ? " " : promptOptionsModel.Prompt,
                NegativePrompt = promptOptionsModel.NegativePrompt,
                DiffuserType = DiffuserType.ImageToImage,
                InputImage = new InputImage(videoFrame),
                VideoInputFPS = promptOptionsModel.VideoInputFPS,
                VideoOutputFPS = promptOptionsModel.VideoOutputFPS,
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
        private async Task<VideoResultModel> GenerateResultAsync(byte[] videoBytes, StableDiffusionModelSetViewModel modelOptions, PromptOptions promptOptions, SchedulerOptions schedulerOptions, long timestamp)
        {
            var tempVideoFile = Path.Combine(".temp", $"VideoToVideo-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.mp4");
            await File.WriteAllBytesAsync(tempVideoFile, videoBytes);
            var videoInfo = await _videoService.GetVideoInfoAsync(videoBytes);
            var videoResult = new VideoResultModel
            {
                Model = modelOptions,
                FileName = tempVideoFile,
                VideoInfo = videoInfo,
                VideoBytes = videoBytes,
                PromptOptions = promptOptions,
                SchedulerOptions = schedulerOptions,
                PipelineType = _selectedModel.ModelSet.PipelineType,
                Elapsed = Stopwatch.GetElapsedTime(timestamp).TotalSeconds
            };
            return videoResult;
        }


        /// <summary>
        /// Saves the video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="schedulerOptions">The scheduler options.</param>
        private async Task SaveVideoFile(VideoResultModel videoResult)
        {
            try
            {
                _logger.LogInformation($"Saving video file...");
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "mp4 files (*.mp4)|*.mp4",
                    DefaultExt = "mp4",
                    AddExtension = true,
                    RestoreDirectory = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    FileName = $"video-{videoResult.SchedulerOptions.Seed}.mp4"
                };

                var dialogResult = saveFileDialog.ShowDialog();
                if (dialogResult == false)
                {
                    _logger.LogInformation("Saving video file canceled");
                    return;
                }

                // Write File
                await File.WriteAllBytesAsync(saveFileDialog.FileName, videoResult.VideoBytes);
                _logger.LogInformation($"Video file saved, File: {saveFileDialog.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving video: {ex.Message}");
            }
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

                App.UIInvoke(() =>
                {
                    if (_cancelationTokenSource.IsCancellationRequested)
                        return;

                    if (progress.BatchTensor is null)
                        ProgressText = $"Frame: {progress.BatchValue:D2}/{_videoFrames.Frames.Count}  |  Step: {progress.StepValue:D2}/{progress.StepMax:D2}";

                    if (progress.BatchTensor is not null)
                    {
                        PreviewResult = Utils.CreateBitmap(progress.BatchTensor.ToImageBytes());
                        PreviewSource = UpdatePreviewFrame(progress.BatchValue - 1);
                    }

                    if (ProgressText != progress.Message && progress.BatchMax == 0)
                        ProgressText = progress.Message;

                    if (ProgressMax != progress.BatchMax)
                        ProgressMax = progress.BatchMax;

                    if (ProgressValue != progress.BatchValue)
                        ProgressValue = progress.BatchValue;

                }, DispatcherPriority.Background);
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
                });
            };
        }

        public BitmapImage UpdatePreviewFrame(int index)
        {
            var frame = _videoFrames.Frames[index];
            using (var memoryStream = new MemoryStream())
            using (var frameImage = SixLabors.ImageSharp.Image.Load<Rgba32>(frame))
            {
                ImageHelpers.Resize(frameImage, new[] { 1, 3, _schedulerOptions.Height, _schedulerOptions.Width });
                frameImage.SaveAsPng(memoryStream);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = memoryStream;
                image.EndInit();
                return image;
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