using OnnxStack.Core.Video;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using System;
using System.Text.Json.Serialization;

namespace Amuse.UI.Models
{
    public class VideoResultModel : IBlueprint
    {
        public string ModelName => Model?.Name;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public DiffuserPipelineType PipelineType { get; init; }
        public DiffuserType DiffuserType { get; init; }
        public PromptOptions PromptOptions { get; init; }
        public SchedulerOptions SchedulerOptions { get; init; }
        public double Elapsed { get; init; }

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public OnnxVideo Video { get; set; }

        [JsonIgnore]
        public StableDiffusionModelSetViewModel Model { get; set; }
        
    }
}