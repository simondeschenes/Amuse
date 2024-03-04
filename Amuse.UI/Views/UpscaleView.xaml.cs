﻿using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using Models;
using OnnxStack.Core.Image;
using OnnxStack.StableDiffusion.Config;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Amuse.UI.Views
{
    /// <summary>
    /// Interaction logic for UpscaleView.xaml
    /// </summary>
    public partial class UpscaleView : UserControl, INavigatable, INotifyPropertyChanged
    {
        private readonly ILogger<UpscaleView> _logger;
        private readonly IFileService _fileService;
        private readonly IUpscaleService _upscaleService;

        private bool _hasResult;
        private int _progressMax;
        private int _progressValue;
        private bool _isGenerating;
        private int _selectedTabIndex;
        private bool _isControlsEnabled;
        private UpscaleResult _resultImage;
        private UpscaleModelSetViewModel _selectedModel;
        private CancellationTokenSource _cancelationTokenSource = null;
        private BitmapSource _inputImage;
        private string _imageFile;
        private UpscaleInfoModel _upscaleInfo;
        private BatchOptionsModel _batchOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpscaleView"/> class.
        /// </summary>
        public UpscaleView()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _logger = App.GetService<ILogger<UpscaleView>>();
                _fileService = App.GetService<IFileService>();
                _upscaleService = App.GetService<IUpscaleService>();
            }

            CancelCommand = new AsyncRelayCommand(Cancel, CanExecuteCancel);
            GenerateCommand = new AsyncRelayCommand(Generate, CanExecuteGenerate);
            ClearHistoryCommand = new AsyncRelayCommand(ClearHistory, CanExecuteClearHistory);
            SaveImageCommand = new AsyncRelayCommand<UpscaleResult>(_fileService.SaveAsImageFile);
            RemoveImageCommand = new AsyncRelayCommand<UpscaleResult>(RemoveImage);
            ImageResults = new ObservableCollection<UpscaleResult>();
            UpscaleInfo = new UpscaleInfoModel();
            IsControlsEnabled = true;
            InitializeComponent();
        }

        public AmuseSettings UISettings
        {
            get { return (AmuseSettings)GetValue(UISettingsProperty); }
            set { SetValue(UISettingsProperty, value); }
        }
        public static readonly DependencyProperty UISettingsProperty =
            DependencyProperty.Register("UISettings", typeof(AmuseSettings), typeof(UpscaleView));

        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand GenerateCommand { get; }
        public AsyncRelayCommand ClearHistoryCommand { get; set; }
        public AsyncRelayCommand<UpscaleResult> SaveImageCommand { get; set; }
        public AsyncRelayCommand<UpscaleResult> RemoveImageCommand { get; set; }
        public ObservableCollection<UpscaleResult> ImageResults { get; }

        public UpscaleModelSetViewModel SelectedModel
        {
            get { return _selectedModel; }
            set { _selectedModel = value; NotifyPropertyChanged(); UpdateInfo(); }
        }

        public UpscaleResult ResultImage
        {
            get { return _resultImage; }
            set { _resultImage = value; NotifyPropertyChanged(); }
        }

        public BitmapSource InputImage
        {
            get { return _inputImage; }
            set { _inputImage = value; NotifyPropertyChanged(); }
        }

        public string ImageFile
        {
            get { return _imageFile; }
            set { _imageFile = value; NotifyPropertyChanged(); LoadImage(); }
        }

        public UpscaleInfoModel UpscaleInfo
        {
            get { return _upscaleInfo; }
            set { _upscaleInfo = value; NotifyPropertyChanged(); }
        }

        public BatchOptionsModel BatchOptions
        {
            get { return _batchOptions; }
            set { _batchOptions = value; NotifyPropertyChanged(); }
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

        private ScrollBarVisibility _scrollBarVisibility;

        public ScrollBarVisibility ScrollBarVisibility
        {
            get { return _scrollBarVisibility; }
            set { _scrollBarVisibility = value; NotifyPropertyChanged(); }
        }

        private bool _showFullImage;

        public bool ShowFullImage
        {
            get { return _showFullImage; }
            set { _showFullImage = value; NotifyPropertyChanged(); UpdateScrollBar(); }
        }

        private void UpdateScrollBar()
        {
            ScrollBarVisibility = _showFullImage
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;
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
            InputImage = imageResult.Image;
            UpdateInfo();
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


            try
            {
                var timestamp = Stopwatch.GetTimestamp();
                var resultImage = await _upscaleService.GenerateAsync(SelectedModel.ModelSet, new OnnxImage(InputImage.GetImageBytes()));
                if (resultImage != null)
                {
                    var elapsed = Stopwatch.GetElapsedTime(timestamp).TotalSeconds;
                    var imageResult = new UpscaleResult(await resultImage.ToBitmapAsync(), UpscaleInfo with { }, elapsed);
                    ResultImage = imageResult;
                    HasResult = true;

                    await _fileService.AutoSaveImageFile(imageResult, "Upscale");
                    ImageResults.Add(imageResult);
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
            return !IsGenerating && InputImage is not null;
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


        private Task RemoveImage(UpscaleResult result)
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
        }

        private void LoadImage()
        {
            InputImage = string.IsNullOrEmpty(_imageFile)
                ? null
                : new BitmapImage(new Uri(_imageFile));
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            if (SelectedModel != null)
            {
                UpscaleInfo.SampleSize = SelectedModel.ModelSet.UpscaleModelConfig.SampleSize;
                UpscaleInfo.ScaleFactor = SelectedModel.ModelSet.UpscaleModelConfig.ScaleFactor;
                UpscaleInfo.InputWidth = InputImage?.PixelWidth ?? SelectedModel.ModelSet.UpscaleModelConfig.SampleSize;
                UpscaleInfo.InputHeight = InputImage?.PixelHeight ?? SelectedModel.ModelSet.UpscaleModelConfig.SampleSize;
                return;
            }
            UpscaleInfo = new UpscaleInfoModel();
        }

        //private async Task<ImageResult> GenerateResultAsync(byte[] imageBytes, long timestamp)
        //{
        //    var image = Utils.CreateBitmap(imageBytes);

        //    //var imageResult = new ImageResult
        //    //{
        //    //    Image = image,
        //    //    Model = _selectedModel,

        //    //    Elapsed = Stopwatch.GetElapsedTime(timestamp).TotalSeconds
        //    //};

        //    if (UISettings.ImageAutoSave)
        //        await imageResult.AutoSaveAsync(Path.Combine(UISettings.ImageAutoSaveDirectory, "TextToImage"), UISettings.ImageAutoSaveBlueprint);
        //    return imageResult;
        //}


        /// <summary>
        /// StableDiffusion progress callback.
        /// </summary>
        /// <returns></returns>
        private Action<int, int> ProgressCallback()
        {
            return (value, maximum) =>
            {
                App.UIInvoke(() =>
                {
                    if (_cancelationTokenSource.IsCancellationRequested)
                        return;

                    if (ProgressMax != maximum)
                        ProgressMax = maximum;

                    ProgressValue = value;
                });
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