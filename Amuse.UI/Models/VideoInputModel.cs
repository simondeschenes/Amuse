using OnnxStack.Core.Video;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using System;
using System.Text.Json.Serialization;

namespace Amuse.UI.Models
{
    public class VideoInputModel
    {
        public VideoInfo VideoInfo { get; set; }
        public string FileName { get; set; }
        public byte[] VideoBytes { get; set; }
    }

    public class VideoResultModel
    {
        public string ModelName => Model?.Name;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public VideoInfo VideoInfo { get; set; }
        public DiffuserPipelineType PipelineType { get; set; }
        public PromptOptions PromptOptions { get; set; }
        public SchedulerOptions SchedulerOptions { get; set; }
        public double Elapsed { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public byte[] VideoBytes { get; set; }

        [JsonIgnore]
        public StableDiffusionModelSetViewModel Model { get; set; }
    }
}