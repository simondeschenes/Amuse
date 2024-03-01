using Amuse.UI.Models;
using Microsoft.Extensions.Logging;
using OnnxStack.Core;
using OnnxStack.Core.Config;
using OnnxStack.Core.Image;
using OnnxStack.Core.Video;
using OnnxStack.FeatureExtractor.Pipelines;
using OnnxStack.StableDiffusion.Common;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using OnnxStack.StableDiffusion.Models;
using OnnxStack.StableDiffusion.Pipelines;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Amuse.UI.Services
{
    /// <summary>
    /// Service for generating images using text and image based prompts
    /// </summary>
    /// <seealso cref="OnnxStack.StableDiffusion.Common.IStableDiffusionService" />
    public sealed class StableDiffusionService : IStableDiffusionService
    {
        private readonly ILogger<StableDiffusionService> _logger;
        private readonly AmuseSettings _settings;
        private readonly Dictionary<IOnnxModel, IPipeline> _pipelines;
        private readonly ConcurrentDictionary<IOnnxModel, ControlNetModel> _controlNetSessions;

        private FeatureExtractorPipeline _featureExtractorPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionService"/> class.
        /// </summary>
        /// <param name="schedulerService">The scheduler service.</param>
        public StableDiffusionService(AmuseSettings settings, ILogger<StableDiffusionService> logger)
        {
            _logger = logger;
            _settings = settings;
            _pipelines = new Dictionary<IOnnxModel, IPipeline>();
            _controlNetSessions = new ConcurrentDictionary<IOnnxModel, ControlNetModel>();
        }


        /// <summary>
        /// Loads the model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public async Task<bool> LoadModelAsync(StableDiffusionModelSet model)
        {
            if (_pipelines.ContainsKey(model))
                return true;

            var pipeline = CreatePipeline(model);
            await pipeline.LoadAsync();
            return _pipelines.TryAdd(model, pipeline);
        }


        /// <summary>
        /// Unloads the model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public async Task<bool> UnloadModelAsync(StableDiffusionModelSet model)
        {
            if (_featureExtractorPipeline is not null)
            {
                await _featureExtractorPipeline.UnloadAsync();
                _featureExtractorPipeline = null;
            }

            if (_pipelines.Remove(model, out var pipeline))
            {
                await pipeline?.UnloadAsync();
            }
            return true;
        }


        /// <summary>
        /// Determines whether [is model loaded] [the specified model options].
        /// </summary>
        /// <param name="modelOptions">The model options.</param>
        /// <returns>
        ///   <c>true</c> if [is model loaded] [the specified model options]; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool IsModelLoaded(StableDiffusionModelSet modelOptions)
        {
            return _pipelines.ContainsKey(modelOptions);
        }


        /// <summary>
        /// Loads the model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> LoadControlNetModelAsync(ControlNetModelSet model)
        {
            if (_controlNetSessions.ContainsKey(model))
                return true;

            var config = model.ControlNetConfig.ApplyDefaults(model);
            var controlNet = new ControlNetModel(config);
            await controlNet.LoadAsync();
            return _controlNetSessions.TryAdd(model, controlNet);
        }


        /// <summary>
        /// Unloads the model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> UnloadControlNetModelAsync(ControlNetModelSet model)
        {
            if (_featureExtractorPipeline is not null)
            {
                await _featureExtractorPipeline.UnloadAsync();
                _featureExtractorPipeline = null;
            }

            if (_controlNetSessions.Remove(model, out var controlNet))
            {
                await controlNet?.UnloadAsync();
            }
            return true;
        }


        /// <summary>
        /// Determines whether the specified model is loaded
        /// </summary>
        /// <param name="modelOptions">The model options.</param>
        /// <returns>
        ///   <c>true</c> if the specified model is loaded; otherwise, <c>false</c>.
        /// </returns>
        public bool IsControlNetModelLoaded(ControlNetModelSet modelOptions)
        {
            return _controlNetSessions.ContainsKey(modelOptions);
        }


        /// <summary>
        /// Generates the StableDiffusion image using the prompt and options provided.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="options">The Scheduler options.</param>
        /// <param name="progressCallback">The callback used to provide progess of the current InferenceSteps.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The diffusion result as <see cref="DenseTensor<float>"/></returns>
        public async Task<OnnxImage> GenerateImageAsync(ModelOptions model, PromptOptions prompt, SchedulerOptions options, Action<DiffusionProgress> progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (!_pipelines.TryGetValue(model.BaseModel, out var pipeline))
                throw new Exception("Pipeline not found or is unsupported");

            pipeline.ValidateInputs(prompt, options);

            var controlNet = default(ControlNetModel);
            if (model.ControlNetModel is not null && !_controlNetSessions.TryGetValue(model.ControlNetModel, out controlNet))
                throw new Exception("ControlNet not loaded");

            await ProcessInputImage(model, prompt, cancellationToken);

            return await pipeline.GenerateImageAsync(prompt, options, controlNet, progressCallback, cancellationToken);
        }


        /// <summary>
        /// Generates a batch of StableDiffusion images using the prompt and options provided.
        /// </summary>
        /// <param name="batchOptions">The batch options.</param>
        /// <param name="model">The model.</param>
        /// <param name="prompt">The prompt.</param>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Pipeline not found or is unsupported
        /// or
        /// ControlNet not loaded
        /// </exception>
        public async IAsyncEnumerable<BatchResult> GenerateImageAsync(BatchOptions batchOptions, ModelOptions model, PromptOptions prompt, SchedulerOptions options, Action<DiffusionProgress> progressCallback = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_pipelines.TryGetValue(model.BaseModel, out var pipeline))
                throw new Exception("Pipeline not found or is unsupported");

            pipeline.ValidateInputs(prompt, options);

            var controlNet = default(ControlNetModel);
            if (model.ControlNetModel is not null && !_controlNetSessions.TryGetValue(model.ControlNetModel, out controlNet))
                throw new Exception("ControlNet not loaded");

            await ProcessInputImage(model, prompt, cancellationToken);

            await foreach (var result in pipeline.RunBatchAsync(batchOptions, prompt, options, controlNet, progressCallback, cancellationToken))
            {
                yield return result;
            }
        }


        /// <summary>
        /// Generates the StableDiffusion video using the prompt and options provided.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="prompt">The prompt.</param>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Pipeline not found or is unsupported
        /// or
        /// ControlNet not loaded
        /// </exception>
        public async Task<OnnxVideo> GenerateVideoAsync(ModelOptions model, PromptOptions prompt, SchedulerOptions options, Action<DiffusionProgress> progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (!_pipelines.TryGetValue(model.BaseModel, out var pipeline))
                throw new Exception("Pipeline not found or is unsupported");

            pipeline.ValidateInputs(prompt, options);

            var controlNet = default(ControlNetModel);
            if (model.ControlNetModel is not null && !_controlNetSessions.TryGetValue(model.ControlNetModel, out controlNet))
                throw new Exception("ControlNet not loaded");

            await ProcessInputVideo(model, prompt, cancellationToken);

            return await pipeline.GenerateVideoAsync(prompt, options, controlNet, progressCallback, cancellationToken);
        }


        /// <summary>
        /// Generates a batch of StableDiffusion videos using the prompt and options provided.
        /// </summary>
        /// <param name="batchOptions"></param>
        /// <param name="model">The model.</param>
        /// <param name="prompt">The prompt.</param>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Pipeline not found or is unsupported
        /// or
        /// ControlNet not loaded
        /// </exception>
        public async IAsyncEnumerable<BatchResult> GenerateVideoAsync(BatchOptions batchOptions, ModelOptions model, PromptOptions prompt, SchedulerOptions options, Action<DiffusionProgress> progressCallback, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!_pipelines.TryGetValue(model.BaseModel, out var pipeline))
                throw new Exception("Pipeline not found or is unsupported");

            pipeline.ValidateInputs(prompt, options);

            var controlNet = default(ControlNetModel);
            if (model.ControlNetModel is not null && !_controlNetSessions.TryGetValue(model.ControlNetModel, out controlNet))
                throw new Exception("ControlNet not loaded");

            await ProcessInputVideo(model, prompt, cancellationToken);

            await foreach (var result in pipeline.RunBatchAsync(batchOptions, prompt, options, controlNet, progressCallback, cancellationToken))
            {
                yield return result;
            }
        }


        /// <summary>
        /// Creates the pipeline.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        private IPipeline CreatePipeline(StableDiffusionModelSet model)
        {
            return model.PipelineType switch
            {
                DiffuserPipelineType.StableDiffusion => StableDiffusionPipeline.CreatePipeline(model, _logger),
                DiffuserPipelineType.StableDiffusionXL => StableDiffusionXLPipeline.CreatePipeline(model, _logger),
                DiffuserPipelineType.LatentConsistency => LatentConsistencyPipeline.CreatePipeline(model, _logger),
                DiffuserPipelineType.LatentConsistencyXL => LatentConsistencyXLPipeline.CreatePipeline(model, _logger),
                DiffuserPipelineType.InstaFlow => InstaFlowPipeline.CreatePipeline(model, _logger),
                _ => throw new NotSupportedException()
            }; ;
        }


        private async Task ProcessInputImage(ModelOptions model, PromptOptions prompt, CancellationToken cancellationToken)
        {
            if (model.FeatureExtractorModel is not null)
            {
                if (_featureExtractorPipeline is null || _featureExtractorPipeline.Name != model.FeatureExtractorModel.Name)
                {
                    if (_featureExtractorPipeline is not null)
                    {
                        await _featureExtractorPipeline.UnloadAsync();
                        _featureExtractorPipeline = null;
                    }

                    _featureExtractorPipeline = FeatureExtractorPipeline.CreatePipeline(model.FeatureExtractorModel, _logger);
                }

                if (_featureExtractorPipeline is not null)
                {
                    await _featureExtractorPipeline.LoadAsync();
                    prompt.InputContolImage = await _featureExtractorPipeline.RunAsync(prompt.InputContolImage, cancellationToken);
                    if (_settings.DefaultMemoryMode == MemoryModeType.Minimum)
                        await _featureExtractorPipeline.UnloadAsync();
                }
            }
        }


        private async Task ProcessInputVideo(ModelOptions model, PromptOptions prompt, CancellationToken cancellationToken)
        {
            if (model.FeatureExtractorModel is not null)
            {
                if (_featureExtractorPipeline is null || _featureExtractorPipeline.Name != model.FeatureExtractorModel.Name)
                {
                    if (_featureExtractorPipeline is not null)
                    {
                        await _featureExtractorPipeline.UnloadAsync();
                        _featureExtractorPipeline = null;
                    }

                    await _featureExtractorPipeline?.UnloadAsync();
                    _featureExtractorPipeline = FeatureExtractorPipeline.CreatePipeline(model.FeatureExtractorModel, _logger);
                }

                if (_featureExtractorPipeline is not null)
                {
                    await _featureExtractorPipeline.LoadAsync();
                    prompt.InputContolVideo = await _featureExtractorPipeline.RunAsync(prompt.InputContolVideo, cancellationToken);
                    if (_settings.DefaultMemoryMode == MemoryModeType.Minimum)
                        await _featureExtractorPipeline.UnloadAsync();
                }
            }
        }
    }
}
