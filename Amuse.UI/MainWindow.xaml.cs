using Amuse.UI.Commands;
using Amuse.UI.Models;
using Amuse.UI.Views;
using Microsoft.Extensions.Logging;
using OnnxStack.ImageUpscaler.Config;
using OnnxStack.StableDiffusion.Config;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace Amuse.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _outputLog;
        private int _selectedTabIndex;
        private INavigatable _selectedTabItem;
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(AmuseSettings uiSettings, ILogger<MainWindow> logger)
        {
            _logger = logger;
            UISettings = uiSettings;
            TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo();
            NavigateTextToImageCommand = new AsyncRelayCommand<ImageResult>(NavigateTextToImage);
            NavigateImageToImageCommand = new AsyncRelayCommand<ImageResult>(NavigateImageToImage);
            NavigateImageInpaintCommand = new AsyncRelayCommand<ImageResult>(NavigateImageInpaint);
            NavigateImagePaintToImageCommand = new AsyncRelayCommand<ImageResult>(NavigateImagePaintToImage);
            NavigateUpscalerCommand = new AsyncRelayCommand<ImageResult>(NavigateUpscaler);
            WindowCloseCommand = new AsyncRelayCommand(WindowClose);
            WindowRestoreCommand = new AsyncRelayCommand(WindowRestore);
            WindowMinimizeCommand = new AsyncRelayCommand(WindowMinimize);
            WindowMaximizeCommand = new AsyncRelayCommand(WindowMaximize);
            InitializeComponent();
            Title = $"Amuse - {Utils.GetAppVersion()}";
            _logger.LogInformation($"Amuse {Utils.GetAppVersion()} successfully started.");
        }



        public AsyncRelayCommand WindowMinimizeCommand { get; }
        public AsyncRelayCommand WindowRestoreCommand { get; }
        public AsyncRelayCommand WindowMaximizeCommand { get; }
        public AsyncRelayCommand WindowCloseCommand { get; }
        public AsyncRelayCommand<ImageResult> NavigateTextToImageCommand { get; }
        public AsyncRelayCommand<ImageResult> NavigateImageToImageCommand { get; }
        public AsyncRelayCommand<ImageResult> NavigateImageInpaintCommand { get; }
        public AsyncRelayCommand<ImageResult> NavigateImagePaintToImageCommand { get; }
        public AsyncRelayCommand<ImageResult> NavigateUpscalerCommand { get; }

        public AmuseSettings UISettings
        {
            get { return (AmuseSettings)GetValue(UISettingsProperty); }
            set { SetValue(UISettingsProperty, value); }
        }
        public static readonly DependencyProperty UISettingsProperty =
            DependencyProperty.Register("UISettings", typeof(AmuseSettings), typeof(MainWindow));


        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set { _selectedTabIndex = value; NotifyPropertyChanged(); }
        }

        public INavigatable SelectedTabItem
        {
            get { return _selectedTabItem; }
            set { _selectedTabItem = value; NotifyPropertyChanged(); }
        }

        private async Task NavigateTextToImage(ImageResult result)
        {
            await NavigateToTab(TabId.TextToImage, result);
        }

        private async Task NavigateImageToImage(ImageResult result)
        {
            await NavigateToTab(TabId.ImageToImage, result);
        }

        private async Task NavigateImageInpaint(ImageResult result)
        {
            await NavigateToTab(TabId.ImageInpaint, result);
        }

        private async Task NavigateImagePaintToImage(ImageResult result)
        {
            await NavigateToTab(TabId.PaintToImage, result);
        }

        private async Task NavigateUpscaler(ImageResult result)
        {
            await NavigateToTab(TabId.Upscaler, result);
        }

        private async Task NavigateToTab(TabId tab, ImageResult imageResult)
        {
            SelectedTabIndex = (int)tab;
            await SelectedTabItem.NavigateAsync(imageResult);
        }

        private enum TabId
        {
            TextToImage = 0,
            ImageToImage = 1,
            ImageInpaint = 2,
            PaintToImage = 3,
            VideoToVideo = 4,
            Upscaler = 5
        }


        /// <summary>
        /// Gets or sets the output log.
        /// </summary>
        public string OutputLog
        {
            get { return _outputLog; }
            set { _outputLog = value; NotifyPropertyChanged(); }
        }


        /// <summary>
        /// Updates the output log.
        /// </summary>
        /// <param name="message">The message.</param>
        public void UpdateOutputLog(string message)
        {
            OutputLog += message;
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
