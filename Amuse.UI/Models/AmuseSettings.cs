using Microsoft.ML.OnnxRuntime;
using OnnxStack.Common.Config;
using OnnxStack.Core.Config;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.Json.Serialization;

namespace Amuse.UI.Models
{
    public class AmuseSettings : IConfigSection
    {
        public ModelCacheMode ModelCacheMode { get; set; }
        public bool AutoSaveImage { get; set; }
        public bool AutoSaveVideo { get; set; }
        public bool AutoSaveBlueprint { get; set; }
        public string DirectoryModel { get; set; }
        public string DirectoryImage { get; set; }
        public string DirectoryImageSave { get; set; }
        public string DirectoryImageAutoSave { get; set; }
        public string DirectoryVideo { get; set; }
        public string DirectoryVideoSave { get; set; }
        public string DirectoryVideoAutoSave { get; set; }
        public int RealtimeRefreshRate { get; set; } = 100;
        public bool RealtimeHistoryEnabled { get; set; } = true;
        public int DefaultDeviceId { get; set; }
        public int DefaultInterOpNumThreads { get; set; }
        public int DefaultIntraOpNumThreads { get; set; }
        public ExecutionMode DefaultExecutionMode { get; set; }
        public ExecutionProvider DefaultExecutionProvider { get; set; }
        public ObservableCollection<ModelTemplateViewModel> Templates { get; set; } = new ObservableCollection<ModelTemplateViewModel>();
        public ObservableCollection<UpscaleModelSetViewModel> UpscaleModelSets { get; set; } = new ObservableCollection<UpscaleModelSetViewModel>();
        public ObservableCollection<StableDiffusionModelSetViewModel> StableDiffusionModelSets { get; set; } = new ObservableCollection<StableDiffusionModelSetViewModel>();
        public ObservableCollection<ControlNetModelSetViewModel> ControlNetModelSets { get; set; } = new ObservableCollection<ControlNetModelSetViewModel>();

        [JsonIgnore]
        public string DirectoryTemp { get; set; }

        [JsonIgnore]
        public string DirectoryCache { get; set; }

        [JsonIgnore]
        public ExecutionProvider SupportedExecutionProvider => GetSupportedExecutionProvider();


        public ExecutionProvider GetSupportedExecutionProvider()
        {
#if DEBUG_CUDA || RELEASE_CUDA
            return ExecutionProvider.Cuda;
#elif DEBUG_TENSORRT || RELEASE_TENSORRT
            return ExecutionProvider.TensorRT;
#else
            return ExecutionProvider.DirectML;
#endif
        }

        public void Initialize()
        {
            DefaultExecutionProvider = DefaultExecutionProvider == SupportedExecutionProvider || DefaultExecutionProvider == ExecutionProvider.Cpu
              ? DefaultExecutionProvider
              : SupportedExecutionProvider;

            if (string.IsNullOrEmpty(DirectoryTemp))
                DirectoryTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".temp");
            if (string.IsNullOrEmpty(DirectoryCache))
                DirectoryCache = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cache");
        }

        public List<string> GetModelNames()
        {
            return Templates
                .Where(x => x.IsUserTemplate)
                .Select(x => x.Name)
                .ToList();
        }
    }

    public enum ModelCacheMode
    {
        Single = 0,
        Multiple = 1
    }
}
