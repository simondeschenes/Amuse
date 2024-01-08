using Amuse.UI.Commands;
using Amuse.UI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Amuse.UI.UserControls
{
    public partial class VideoResultControl : UserControl, INotifyPropertyChanged
    {
        private readonly ILogger<VideoResultControl> _logger;
        private bool _isPreviewVisible;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoResultControl" /> class.
        /// </summary>
        public VideoResultControl()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
                _logger = App.GetService<ILogger<VideoResultControl>>();

            UpdateSeedCommand = new AsyncRelayCommand(UpdateSeed);
            InitializeComponent();
            HasVideoResult = false;
        }

        public AsyncRelayCommand ClearVideoCommand { get; }
        public AsyncRelayCommand UpdateSeedCommand { get; }

        public VideoResultModel VideoResult
        {
            get { return (VideoResultModel)GetValue(VideoResultProperty); }
            set { SetValue(VideoResultProperty, value); }
        }
        public static readonly DependencyProperty VideoResultProperty =
            DependencyProperty.Register("VideoResult", typeof(VideoResultModel), typeof(VideoResultControl));

        public SchedulerOptionsModel SchedulerOptions
        {
            get { return (SchedulerOptionsModel)GetValue(SchedulerOptionsProperty); }
            set { SetValue(SchedulerOptionsProperty, value); }
        }
        public static readonly DependencyProperty SchedulerOptionsProperty =
            DependencyProperty.Register("SchedulerOptions", typeof(SchedulerOptionsModel), typeof(VideoResultControl));

        public bool IsGenerating
        {
            get { return (bool)GetValue(IsGeneratingProperty); }
            set { SetValue(IsGeneratingProperty, value); }
        }
        public static readonly DependencyProperty IsGeneratingProperty =
            DependencyProperty.Register("IsGenerating", typeof(bool), typeof(VideoResultControl), new PropertyMetadata((s, e) =>
            {
                if (s is VideoResultControl control && e.NewValue is bool isGenerating)
                {
                    if (!isGenerating)
                        control.IsPreviewVisible = false;
                }
            }));

        public bool HasVideoResult
        {
            get { return (bool)GetValue(HasVideoResultProperty); }
            set { SetValue(HasVideoResultProperty, value); }
        }
        public static readonly DependencyProperty HasVideoResultProperty =
            DependencyProperty.Register("HasVideoResult", typeof(bool), typeof(VideoResultControl));

        public int ProgressMax
        {
            get { return (int)GetValue(ProgressMaxProperty); }
            set { SetValue(ProgressMaxProperty, value); }
        }
        public static readonly DependencyProperty ProgressMaxProperty =
            DependencyProperty.Register("ProgressMax", typeof(int), typeof(VideoResultControl));

        public int ProgressValue
        {
            get { return (int)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }
        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register("ProgressValue", typeof(int), typeof(VideoResultControl));

        public string ProgressText
        {
            get { return (string)GetValue(ProgressTextProperty); }
            set { SetValue(ProgressTextProperty, value); }
        }
        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register("ProgressText", typeof(string), typeof(VideoResultControl));

        public BitmapImage PreviewImage
        {
            get { return (BitmapImage)GetValue(PreviewImageProperty); }
            set { SetValue(PreviewImageProperty, value); }
        }
        public static readonly DependencyProperty PreviewImageProperty =
            DependencyProperty.Register("PreviewImage", typeof(BitmapImage), typeof(VideoResultControl), new PropertyMetadata((s, e) =>
            {
                if (s is VideoResultControl control && !control.IsPreviewVisible)
                    control.IsPreviewVisible = true;
            }));

        public bool IsPreviewVisible
        {
            get { return _isPreviewVisible; }
            set { _isPreviewVisible = value; NotifyPropertyChanged(); }
        }


        private Task UpdateSeed()
        {
            SchedulerOptions.Seed = VideoResult.SchedulerOptions.Seed;
            return Task.CompletedTask;
        }


        /// <summary>
        /// Handles the Loaded event of the MediaElement control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void MediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MediaElement mediaElement)
                return;

            mediaElement.LoadedBehavior = MediaState.Play;
        }


        /// <summary>
        /// Handles the MediaEnded event of the MediaElement control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is not MediaElement mediaElement)
                return;

            mediaElement.Position = TimeSpan.FromMilliseconds(1);
        }


        /// <summary>
        /// Handles the MouseDown event of the MediaElement control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void MediaElement_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not MediaElement mediaElement)
                return;

            mediaElement.LoadedBehavior = mediaElement.LoadedBehavior == MediaState.Pause
                ? MediaState.Play
                : MediaState.Pause;
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
