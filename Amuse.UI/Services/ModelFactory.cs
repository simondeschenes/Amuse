using Amuse.UI.Models;
using OnnxStack.FeatureExtractor.Common;
using OnnxStack.ImageUpscaler.Common;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using OnnxStack.StableDiffusion.Models;
using System;
using System.IO;
using System.Linq;

namespace Amuse.UI.Services
{
    public class ModelFactory : IModelFactory
    {
        private readonly AmuseSettings _settings;
        private readonly string _defaultTokenizerPath;

        public ModelFactory(AmuseSettings settings)
        {
            _settings = settings;
            var defaultTokenizerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cliptokenizer.onnx");
            if (File.Exists(defaultTokenizerPath))
                _defaultTokenizerPath = defaultTokenizerPath;
        }

        public StableDiffusionModelSet CreateStableDiffusionModelSet(string name, string folder, string modelTemplateType)
        {
            var template = _settings.Templates
                .Where(x => x.Category == ModelTemplateCategory.StableDiffusion && x.Template == modelTemplateType && !x.IsUserTemplate)
                .FirstOrDefault();
            if (template == null)
                return null;

            return CreateStableDiffusionModelSet(name, folder, template.StableDiffusionTemplate);
        }

        public StableDiffusionModelSet CreateStableDiffusionModelSet(string name, string folder, StableDiffusionModelTemplate modelTemplate)
        {
            var modelSet = new StableDiffusionModelSet
            {
                Name = name,
                PipelineType = modelTemplate.PipelineType,
                Diffusers = modelTemplate.DiffuserTypes.ToList(),
                SampleSize = modelTemplate.SampleSize,
                DeviceId = _settings.DefaultDeviceId,
                ExecutionMode = _settings.DefaultExecutionMode,
                ExecutionProvider = _settings.DefaultExecutionProvider,
                InterOpNumThreads = _settings.DefaultInterOpNumThreads,
                IntraOpNumThreads = _settings.DefaultIntraOpNumThreads,
                MemoryMode = _settings.DefaultMemoryMode,
                IsEnabled = true,
            };

            // Some repositories have the ControlNet in the unet folder, some on the controlnet folder
            var isControlNet = modelTemplate.DiffuserTypes.Any(x => x == DiffuserType.ControlNet || x == DiffuserType.ControlNetImage);
            var unetPath = Path.Combine(folder, "unet", "model.onnx");
            var controlNetUnetPath = Path.Combine(folder, "controlnet", "model.onnx");
            if (isControlNet && File.Exists(controlNetUnetPath))
                unetPath = controlNetUnetPath;

            var tokenizerPath = Path.Combine(folder, "tokenizer", "model.onnx");
            var textEncoderPath = Path.Combine(folder, "text_encoder", "model.onnx");
            var vaeDecoder = Path.Combine(folder, "vae_decoder", "model.onnx");
            var vaeEncoder = Path.Combine(folder, "vae_encoder", "model.onnx");
            var tokenizer2Path = Path.Combine(folder, "tokenizer_2", "model.onnx");
            var textEncoder2Path = Path.Combine(folder, "text_encoder_2", "model.onnx");
            var controlnet = Path.Combine(folder, "controlnet", "model.onnx");
            if (!File.Exists(tokenizerPath))
                tokenizerPath = _defaultTokenizerPath;
            if (!File.Exists(tokenizer2Path))
                tokenizer2Path = _defaultTokenizerPath;


            if (modelSet.PipelineType == DiffuserPipelineType.StableDiffusionXL || modelSet.PipelineType == DiffuserPipelineType.LatentConsistencyXL)
            {
                if (modelTemplate.ModelType == ModelType.Refiner)
                {
                    modelSet.UnetConfig = new UNetConditionModelConfig { OnnxModelPath = unetPath, ModelType = ModelType.Refiner };
                    modelSet.Tokenizer2Config = new TokenizerModelConfig { OnnxModelPath = tokenizer2Path, TokenizerLength = 1280, PadTokenId = 1 };
                    modelSet.TextEncoder2Config = new TextEncoderModelConfig { OnnxModelPath = textEncoder2Path };
                    modelSet.VaeDecoderConfig = new AutoEncoderModelConfig { OnnxModelPath = vaeDecoder, ScaleFactor = 0.13025f };
                    modelSet.VaeEncoderConfig = new AutoEncoderModelConfig { OnnxModelPath = vaeEncoder, ScaleFactor = 0.13025f };
                }
                else
                {
                    modelSet.UnetConfig = new UNetConditionModelConfig { OnnxModelPath = unetPath, ModelType = ModelType.Base };
                    modelSet.TokenizerConfig = new TokenizerModelConfig { OnnxModelPath = tokenizerPath, PadTokenId = 1 };
                    modelSet.Tokenizer2Config = new TokenizerModelConfig { OnnxModelPath = tokenizer2Path, TokenizerLength = 1280, PadTokenId = 1 };
                    modelSet.TextEncoderConfig = new TextEncoderModelConfig { OnnxModelPath = textEncoderPath };
                    modelSet.TextEncoder2Config = new TextEncoderModelConfig { OnnxModelPath = textEncoder2Path };
                    modelSet.VaeDecoderConfig = new AutoEncoderModelConfig { OnnxModelPath = vaeDecoder, ScaleFactor = 0.13025f };
                    modelSet.VaeEncoderConfig = new AutoEncoderModelConfig { OnnxModelPath = vaeEncoder, ScaleFactor = 0.13025f };
                }
            }
            else
            {
                var tokenizerLength = modelTemplate.ModelType == ModelType.Turbo ? 1024 : 768;
                modelSet.UnetConfig = new UNetConditionModelConfig { OnnxModelPath = unetPath, ModelType = modelTemplate.ModelType };
                modelSet.TokenizerConfig = new TokenizerModelConfig { OnnxModelPath = tokenizerPath, TokenizerLength = tokenizerLength };
                modelSet.TextEncoderConfig = new TextEncoderModelConfig { OnnxModelPath = textEncoderPath };
                modelSet.VaeDecoderConfig = new AutoEncoderModelConfig { OnnxModelPath = vaeDecoder, ScaleFactor = 0.18215f };
                modelSet.VaeEncoderConfig = new AutoEncoderModelConfig { OnnxModelPath = vaeEncoder, ScaleFactor = 0.18215f };
            }

            return modelSet;
        }


        public UpscaleModelSet CreateUpscaleModelSet(string name, string filename, string modelTemplateType)
        {
            var template = _settings.Templates
               .Where(x => x.Category == ModelTemplateCategory.StableDiffusion && x.Template == modelTemplateType && !x.IsUserTemplate)
               .FirstOrDefault();
            if (template == null)
                return null;

            return CreateUpscaleModelSet(name, filename, template.UpscaleTemplate);
        }


        public UpscaleModelSet CreateUpscaleModelSet(string name, string filename, UpscaleModelTemplate modelTemplate)
        {
            return new UpscaleModelSet
            {
                Name = name,
                IsEnabled = true,
                DeviceId = _settings.DefaultDeviceId,
                ExecutionMode = _settings.DefaultExecutionMode,
                ExecutionProvider = _settings.DefaultExecutionProvider,
                InterOpNumThreads = _settings.DefaultInterOpNumThreads,
                IntraOpNumThreads = _settings.DefaultIntraOpNumThreads,
                UpscaleModelConfig = new UpscaleModelConfig
                {
                    Channels = 3,
                    SampleSize = modelTemplate.SampleSize,
                    ScaleFactor = modelTemplate.ScaleFactor,
                    OnnxModelPath = filename
                }
            };
        }

        public ControlNetModelSet CreateControlNetModelSet(string name, ControlNetType controlNetType, DiffuserPipelineType pipelineType, string modelFilename)
        {
            return new ControlNetModelSet
            {
                Name = name,
                Type = controlNetType,
                PipelineType = pipelineType,
                IsEnabled = true,
                DeviceId = _settings.DefaultDeviceId,
                ExecutionMode = _settings.DefaultExecutionMode,
                ExecutionProvider = _settings.DefaultExecutionProvider,
                InterOpNumThreads = _settings.DefaultInterOpNumThreads,
                IntraOpNumThreads = _settings.DefaultIntraOpNumThreads,
                ControlNetConfig = new ControlNetModelConfig
                {
                    OnnxModelPath = modelFilename
                }
            };
        }


        public FeatureExtractorModelSet CreateFeatureExtractorModelSet(string name, bool normalize, int sampleSize, int channels, string modelFilename)
        {
            return new FeatureExtractorModelSet
            {
                Name = name,
                IsEnabled = true,
                DeviceId = _settings.DefaultDeviceId,
                ExecutionMode = _settings.DefaultExecutionMode,
                ExecutionProvider = _settings.DefaultExecutionProvider,
                InterOpNumThreads = _settings.DefaultInterOpNumThreads,
                IntraOpNumThreads = _settings.DefaultIntraOpNumThreads,
                FeatureExtractorConfig = new FeatureExtractorModelConfig
                {
                    Channels = channels,
                    Normalize = normalize,
                    SampleSize = sampleSize,
                    OnnxModelPath = modelFilename
                }
            };
        }
    }
}
