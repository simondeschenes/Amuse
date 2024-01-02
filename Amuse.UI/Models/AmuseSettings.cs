using Microsoft.ML.OnnxRuntime;
using OnnxStack.Common.Config;
using OnnxStack.Core.Config;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Amuse.UI.Models
{
    public class AmuseSettings : IConfigSection
    {
        public ModelCacheMode ModelCacheMode { get; set; }

        public bool ImageAutoSave { get; set; }
        public bool ImageAutoSaveBlueprint { get; set; }
        public string ImageAutoSaveDirectory { get; set; }
        public int RealtimeRefreshRate { get; set; } = 100;
        public bool RealtimeHistoryEnabled { get; set; }
        public int DefaultDeviceId { get; set; }
        public int DefaultInterOpNumThreads { get; set; }
        public int DefaultIntraOpNumThreads { get; set; }
        public ExecutionMode DefaultExecutionMode { get; set; }
        public ExecutionProvider DefaultExecutionProvider { get; set; }

        [JsonIgnore]
        public ExecutionProvider SupportedExecutionProvider => GetSupportedExecutionProvider();

        public ObservableCollection<ModelTemplateViewModel> Templates { get; set; } = new ObservableCollection<ModelTemplateViewModel>();
        public ObservableCollection<UpscaleModelSetViewModel> UpscaleModelSets { get; set; } = new ObservableCollection<UpscaleModelSetViewModel>();
        public ObservableCollection<StableDiffusionModelSetViewModel> StableDiffusionModelSets { get; set; } = new ObservableCollection<StableDiffusionModelSetViewModel>();

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
        }

    }

    public enum ModelCacheMode
    {
        Single = 0,
        Multiple = 1
    }
}
