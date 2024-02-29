using OnnxStack.StableDiffusion.Config;

namespace Amuse.UI.Models
{
    public record ModelOptions(StableDiffusionModelSet BaseModel, ControlNetModelSet ControlNetModel = default);
}
