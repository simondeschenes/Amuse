using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using System;

namespace Amuse.UI.Models
{
    public interface IBlueprint
    {
        string ModelName { get; }
        DateTime Timestamp { get; }
        DiffuserPipelineType PipelineType { get; init; }
        DiffuserType DiffuserType { get; init; }
        PromptOptions PromptOptions { get; init; }
        SchedulerOptions SchedulerOptions { get; init; }
        double Elapsed { get; init; }
    }
}