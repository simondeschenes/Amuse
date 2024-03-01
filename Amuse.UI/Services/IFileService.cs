using Amuse.UI.Models;
using OnnxStack.Core.Video;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Amuse.UI.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Opens the image file.
        /// </summary>
        /// <returns></returns>
        Task<ImageInput> OpenImageFile();

        /// <summary>
        /// Opens the image file via the Crop dialog.
        /// </summary>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="initialImage">The initial image.</param>
        /// <param name="initialImageFile">The initial image file.</param>
        /// <returns></returns>
        Task<ImageInput> OpenImageFileCropped(int maxWidth, int maxHeight, BitmapSource initialImage = null, string initialImageFile = default);

        /// <summary>
        /// Opens a video file.
        /// </summary>
        /// <returns></returns>
        Task<VideoInputModel> OpenVideoFile();

        /// <summary>
        /// Saves the image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        Task SaveImageFile(ImageResult imageResult, string filename);

        /// <summary>
        /// Saves image file via FilePicker dialog.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <returns></returns>
        Task SaveAsImageFile(ImageResult imageResult);

        /// <summary>
        /// Automaticly the save image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        Task AutoSaveImageFile(ImageResult imageResult, string prefix);

        /// <summary>
        /// Saves the blueprint file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        Task SaveBlueprintFile(ImageResult videoResult, string filename);

        /// <summary>
        /// Saves the blueprint file via FilePicker dialog.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <returns></returns>
        Task SaveAsBlueprintFile(ImageResult videoResult);

        /// <summary>
        /// Saves the image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        Task SaveImageFile(UpscaleResult imageResult, string filename);

        /// <summary>
        /// Saves the image file via FilePicker dialog.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <returns></returns>
        Task SaveAsImageFile(UpscaleResult imageResult);

        /// <summary>
        /// Automaticly the save image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        Task AutoSaveImageFile(UpscaleResult imageResult, string prefix);

        /// <summary>
        /// Saves the video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        Task SaveVideoFile(VideoResultModel videoResult, string filename);

        /// <summary>
        /// Saves the video file via FilePicker dialog.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <returns></returns>
        Task SaveAsVideoFile(VideoResultModel videoResult);

        /// <summary>
        /// Automaticly the save video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        Task AutoSaveVideoFile(VideoResultModel videoResult, string prefix);

        /// <summary>
        /// Saves the blueprint file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        Task SaveBlueprintFile(VideoResultModel videoResult, string filename);

        /// <summary>
        /// Saves the blueprint file via FilePicker dialog.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <returns></returns>
        Task SaveAsBlueprintFile(VideoResultModel videoResult);

        /// <summary>
        /// Deletes the temporary files.
        /// </summary>
        /// <returns></returns>
        Task DeleteTempFiles();

        /// <summary>
        /// Saves a temporary video file.
        /// </summary>
        /// <param name="videoBytes">The video bytes.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        Task<string> SaveTempVideoFile(OnnxVideo video, string prefix = default);

        /// <summary>
        /// Deletes the temporary video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <returns></returns>
        Task DeleteTempVideoFile(VideoResultModel videoResult);

        /// <summary>
        /// Deletes the temporary video files.
        /// </summary>
        /// <param name="videoResults">The video results.</param>
        /// <returns></returns>
        Task DeleteTempVideoFile(IEnumerable<VideoResultModel> videoResults);
    }
}