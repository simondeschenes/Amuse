using OnnxStack.Core.Image;
using OnnxStack.ImageUpscaler.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Amuse.UI.Services
{
    public interface IUpscaleService
    {

        /// <summary>
        /// Loads the model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        Task<bool> LoadModelAsync(UpscaleModelSet model);

        /// <summary>
        /// Unloads the model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        Task<bool> UnloadModelAsync(UpscaleModelSet model);

        /// <summary>
        /// Determines whether [is model loaded] [the specified model options].
        /// </summary>
        /// <param name="modelOptions">The model options.</param>
        /// <returns>
        ///   <c>true</c> if [is model loaded] [the specified model options]; otherwise, <c>false</c>.
        /// </returns>
        bool IsModelLoaded(UpscaleModelSet modelOptions);

        /// <summary>
        /// Generates the upscaled image.
        /// </summary>
        /// <param name="modelOptions">The model options.</param>
        /// <param name="inputImage">The input image.</param>
        /// <returns></returns>
        Task<OnnxImage> GenerateAsync(UpscaleModelSet modelOptions, OnnxImage inputImage, CancellationToken cancellationToken = default);
    }
}
