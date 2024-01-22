using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Amuse.UI
{
    public static class Utils
    {
        public static string GetAppVersion()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }



        public static void NavigateToUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public static string RandomString()
        {
            return Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        }

        public static BitmapImage CreateBitmap(byte[] imageBytes)
        {
            using (var memoryStream = new MemoryStream(imageBytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = memoryStream;
                image.EndInit();
                return image;
            }
        }

        public static byte[] GetImageBytes(this BitmapSource image)
        {
            if (image == null)
                return null;

            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        internal static BitmapSource GetImage(Image<Rgba32> inputImage)
        {
            using (var memoryStream = new MemoryStream())
            {
                inputImage.SaveAsPng(memoryStream);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = memoryStream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        internal static async Task RefreshDelay(long startTime, int refreshRate, CancellationToken cancellationToken)
        {
            var endTime = Stopwatch.GetTimestamp();
            var elapsedMilliseconds = (endTime - startTime) * 1000.0 / Stopwatch.Frequency;
            int adjustedDelay = Math.Max(0, refreshRate - (int)elapsedMilliseconds);
            await Task.Delay(adjustedDelay, cancellationToken).ConfigureAwait(false);
        }

        public static void LogToWindow(string message)
        {
            if (System.Windows.Application.Current is null)
                return;

            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                    mainWindow.UpdateOutputLog(message);
            }));
        }


        public static void TaskbarProgress(int value, int maximum)
        {
            if (System.Windows.Application.Current is null)
                return;

            var pecent = maximum == 0 ? 0 : (double)value / maximum;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.TaskbarItemInfo.ProgressState == System.Windows.Shell.TaskbarItemProgressState.None && maximum > 0)
                        mainWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    if (mainWindow.TaskbarItemInfo.ProgressState == System.Windows.Shell.TaskbarItemProgressState.Normal && maximum == 0)
                        mainWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

                    mainWindow.TaskbarItemInfo.ProgressValue = pecent;
                }
            }));
        }

        /// <summary>
        /// Forces the notify collection changed event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        public static void ForceNotifyCollectionChanged<T>(this ObservableCollection<T> collection)
        {
            // Hack: Moving an item will invoke a collection changed event
            collection?.Move(0, 0);
        }

       
    }
}
