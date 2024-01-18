using OnnxStack.StableDiffusion.Enums;

namespace Amuse.UI.Models
{
    public record StableDiffusionModelTemplate(DiffuserPipelineType PipelineType, ModelType ModelType, int SampleSize, DiffuserType[] DiffuserTypes)
    {
        public int TokenizerLength { get; set; } = 768;
        public StableDiffusionSchedulerDefaults SchedulerDefaults { get; set; } = new StableDiffusionSchedulerDefaults();
    }
}
