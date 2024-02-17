using Amuse.UI.Dialogs;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnnxStack.Core;
using OnnxStack.ImageUpscaler;
using Services;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Amuse.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IHost _applicationHost;

        public App()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Services.AddLogging((loggingBuilder) => loggingBuilder.AddWindowLogger());

            // Add OnnxStackStableDiffusion
            builder.Services.AddOnnxStack();
            builder.Services.AddOnnxStackConfig<AmuseSettings>();

            // Add Windows
            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddTransient<MessageDialog>();
            builder.Services.AddTransient<TextInputDialog>();
            builder.Services.AddTransient<CropImageDialog>();
            builder.Services.AddTransient<AddModelDialog>();
            builder.Services.AddTransient<UpdateModelDialog>();
            builder.Services.AddTransient<AddUpscaleModelDialog>();
            builder.Services.AddTransient<UpdateUpscaleModelDialog>();
            builder.Services.AddTransient<UpdateModelSettingsDialog>();
            builder.Services.AddTransient<UpdateModelMetadataDialog>();
            builder.Services.AddTransient<ViewModelMetadataDialog>();
            builder.Services.AddTransient<UpdateUpscaleModelSettingsDialog> ();
            builder.Services.AddTransient<AddControlNetModelDialog>();
            builder.Services.AddTransient<UpdateControlNetModelDialog>();
            builder.Services.AddTransient<AddFeatureExtractorModelDialog>();
            builder.Services.AddTransient<UpdateFeatureExtractorModelDialog>();
            builder.Services.AddSingleton<IModelFactory, ModelFactory>();
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<IModelDownloadService, ModelDownloadService>();
            builder.Services.AddSingleton<IDeviceService, DeviceService>();
            builder.Services.AddSingleton<IFileService, FileService>();
            builder.Services.AddSingleton<IStableDiffusionService, StableDiffusionService>();
            builder.Services.AddSingleton<IUpscaleService, UpscaleService>();

            // Build App
            _applicationHost = builder.Build();
        }


        public static T GetService<T>() => _applicationHost.Services.GetService<T>();

        public static void UIInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Render) => Current.Dispatcher.BeginInvoke(priority, action);


        /// <summary>
        /// Raises the <see cref="E:Startup" /> event.
        /// </summary>
        /// <param name="e">The <see cref="StartupEventArgs"/> instance containing the event data.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await _applicationHost.StartAsync();
            GetService<MainWindow>().Show();
        }


        /// <summary>
        /// Raises the <see cref="E:Exit" /> event.
        /// </summary>
        /// <param name="e">The <see cref="ExitEventArgs"/> instance containing the event data.</param>
        protected override async void OnExit(ExitEventArgs e)
        {
            await GetService<IFileService>().DeleteTempFiles();
            await _applicationHost.StopAsync();
            base.OnExit(e);
        }
    }
}
