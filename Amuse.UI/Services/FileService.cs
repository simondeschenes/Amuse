using Amuse.UI.Dialogs;
using Amuse.UI.Models;
using Amuse.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OnnxStack.Core.Image;
using OnnxStack.Core.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IDialogService _dialogService;
        private readonly AmuseSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="videoService">The video service.</param>
        /// <param name="logger">The logger.</param>
        public FileService(AmuseSettings settings, IDialogService dialogService, ILogger<FileService> logger = default)
        {
            _logger = logger;
            _settings = settings;
            _dialogService = dialogService;
        }

        #region Image Files

        /// <summary>
        /// Opens the image file.
        /// </summary>
        /// <returns></returns>
        public Task<ImageInput> OpenImageFile()
        {
            try
            {
                _logger?.LogInformation($"[OpenImageFile] Opening image file dialog...");
                var imageFile = OpenImageFileDialog();
                if (string.IsNullOrEmpty(imageFile))
                    return Task.FromResult<ImageInput>(default);

                _logger?.LogInformation($"[OpenImageFile] Loading image file: {imageFile}");
                var bitmapImage = new BitmapImage(new Uri(imageFile));
                _logger?.LogInformation($"[OpenImageFile] Image file loaded.");
                return Task.FromResult(new ImageInput
                {
                    Image = bitmapImage,
                    FileName = imageFile
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[OpenImageFile] Error opening image file: {ex.Message}");
                return Task.FromResult<ImageInput>(default);
            }
        }


        /// <summary>
        /// Opens the image file via the Crop dialog.
        /// </summary>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="initialImage">The initial image.</param>
        /// <param name="initialImageFile">The initial image file.</param>
        /// <returns></returns>
        public Task<ImageInput> OpenImageFileCropped(int maxWidth, int maxHeight, BitmapSource initialImage = default, string initialImageFile = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(initialImageFile))
                    initialImage = new BitmapImage(new Uri(initialImageFile));


                _logger?.LogInformation($"[OpenImageFile] Opening image crop dialog...");
                var loadImageDialog = _dialogService.GetDialog<CropImageDialog>();
                loadImageDialog.Initialize(maxWidth, maxHeight, initialImage);
                if (loadImageDialog.ShowDialog() != true)
                {
                    _logger?.LogInformation($"[OpenImageFile] Loading image crop canceled");
                    return Task.FromResult<ImageInput>(default);
                }

                _logger?.LogInformation($"[OpenImageFile] Image file cropped and loaded, {maxWidth}x{maxHeight}, {loadImageDialog.ImageFile}");
                return Task.FromResult(new ImageInput
                {
                    Image = loadImageDialog.GetImageResult(),
                    FileName = loadImageDialog.ImageFile,
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[OpenImageFileCropped] Error opening image file: {ex.Message}");
                return Task.FromResult<ImageInput>(default);
            }
        }


        /// <summary>
        /// Automaticly the save image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="prefix">The prefix.</param>
        public async Task AutoSaveImageFile(ImageResult imageResult, string prefix)
        {
            try
            {
                if (!_settings.AutoSaveImage)
                    return;

                var imageFilename = Path.Combine(_settings.DirectoryImageAutoSave, GetRandomFileName("png", prefix));
                _logger?.LogInformation($"[AutoSaveImageFile] Saving {imageResult.PipelineType} image, File: {imageFilename}");
                await SaveImageFileAsync(imageResult.Image, imageFilename);
                if (_settings.AutoSaveBlueprint)
                    await SaveBlueprintFileAsync(imageResult, imageFilename.Replace(".png", ".json"));

                _logger?.LogInformation($"[AutoSaveImageFile] {imageResult.PipelineType} file saved");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[AutoSaveImageFile] Error saving {imageResult.PipelineType} image: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="filename">The filename.</param>
        public async Task SaveImageFile(ImageResult imageResult, string filename)
        {
            try
            {
                _logger?.LogInformation($"[SaveImageFile] Saving {imageResult.PipelineType} image, File: {filename}");
                await SaveImageFileAsync(imageResult.Image, filename);
                _logger?.LogInformation($"[SaveImageFile] {imageResult.PipelineType} file saved");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveImageFile] Error saving {imageResult.PipelineType} image: {ex.Message}");
            }
        }


        /// <summary>
        /// Automaticly the save image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="prefix">The prefix.</param>
        public async Task AutoSaveImageFile(UpscaleResult imageResult, string prefix)
        {
            try
            {
                if (!_settings.AutoSaveImage)
                    return;

                var imageFilename = Path.Combine(_settings.DirectoryImageAutoSave, GetRandomFileName("png", prefix));
                _logger?.LogInformation($"[AutoSaveImageFile] Saving Upscale image, File: {imageFilename}");
                await SaveImageFileAsync(imageResult.Image, imageFilename);
                _logger?.LogInformation($"[AutoSaveImageFile] Upscale file saved");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[AutoSaveImageFile] Error saving Upscale image: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the image file.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        /// <param name="filename">The filename.</param>
        public async Task SaveImageFile(UpscaleResult imageResult, string filename)
        {
            try
            {
                _logger?.LogInformation($"[SaveImageFile] Saving Upscale image, File: {filename}");
                await SaveImageFileAsync(imageResult.Image, filename);
                _logger?.LogInformation($"[SaveImageFile] Upscale file saved");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveImageFile] Error saving Upscale image: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves image file via FilePicker dialog.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        public async Task SaveAsImageFile(ImageResult imageResult)
        {
            try
            {
                _logger?.LogInformation($"[SaveAsImageFile] Saving {imageResult.PipelineType} image file...");
                var saveFileName = SaveImageFileDialog($"{imageResult.SchedulerOptions.Seed}");
                if (string.IsNullOrEmpty(saveFileName))
                    return;

                await SaveImageFileAsync(imageResult.Image, saveFileName);
                _logger?.LogInformation($"[SaveAsImageFile] {imageResult.PipelineType} file saved, File: {saveFileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveAsImageFile] Error saving {imageResult.PipelineType} image: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the image file via FilePicker dialog.
        /// </summary>
        /// <param name="imageResult">The image result.</param>
        public async Task SaveAsImageFile(UpscaleResult imageResult)
        {
            try
            {
                _logger?.LogInformation($"[SaveAsImageFile] Saving Upscale image file...");
                var saveFileName = SaveImageFileDialog($"{imageResult.Info.OutputWidth}x{imageResult.Info.OutputHeight}");
                if (string.IsNullOrEmpty(saveFileName))
                    return;

                await SaveImageFileAsync(imageResult.Image, saveFileName);
                _logger?.LogInformation($"[SaveAsImageFile] Upscale file saved, File: {saveFileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveAsImageFile] Error saving Upscale image: {ex.Message}");
            }
        }


        /// <summary>
        /// Opens the image file dialog.
        /// </summary>
        /// <returns></returns>
        private string OpenImageFileDialog()
        {
            // Show Dialog
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open Image",
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif;*.tiff|All Files|*.*",
                InitialDirectory = _settings.DirectoryImage,
                RestoreDirectory = string.IsNullOrEmpty(_settings.DirectoryImage),
                Multiselect = false,
            };
            if (openFileDialog.ShowDialog() != true)
            {
                _logger?.LogInformation("[OpenImageFileDialog] Open image file canceled");
                return null;
            }

            return openFileDialog.FileName;
        }


        /// <summary>
        /// Saves the image file dialog.
        /// </summary>
        /// <param name="initialFilename">The initial filename.</param>
        /// <returns></returns>
        private string SaveImageFileDialog(string initialFilename)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Image",
                Filter = "png files (*.png)|*.png",
                DefaultExt = "png",
                AddExtension = true,
                RestoreDirectory = string.IsNullOrEmpty(_settings.DirectoryImageSave),
                InitialDirectory = _settings.DirectoryImageSave,
                FileName = $"image-{initialFilename}.png"
            };

            var dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult == false)
            {
                _logger?.LogInformation("[SaveImageFileDialog] Saving image canceled");
                return null;
            }

            return saveFileDialog.FileName;
        }


        /// <summary>
        /// Saves the image file.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        private static Task<bool> SaveImageFileAsync(BitmapSource image, string filename)
        {
            return Task.Run(() =>
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (var fileStream = new FileStream(filename, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
                return File.Exists(filename);
            });
        }

        #endregion

        #region Video Files

        /// <summary>
        /// Opens a video file.
        /// </summary>
        /// <returns></returns>
        public async Task<VideoInputModel> OpenVideoFile()
        {
            try
            {
                _logger?.LogInformation($"[OpenVideoFile] Opening video file dialog...");
                var videoFile = OpenVideoFileDialog();
                if (string.IsNullOrEmpty(videoFile))
                    return null;

                _logger?.LogInformation($"[OpenVideoFile] Loading video file: {videoFile}");
                var videoBytes = await File.ReadAllBytesAsync(videoFile);
                var videoInfo = await VideoHelper.ReadVideoInfoAsync(videoBytes);
                _logger?.LogInformation($"[OpenVideoFile] Video file loaded, {videoInfo}");
                return new VideoInputModel
                {
                    FileName = videoFile,
                    Video = new OnnxVideo(videoInfo, new List<OnnxImage>())
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[OpenVideoFile] Error opening video file: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Automaticly the save video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="prefix">The prefix.</param>
        public async Task AutoSaveVideoFile(VideoResultModel videoResult, string prefix)
        {
            try
            {
                if (!_settings.AutoSaveVideo)
                    return;

                var videoFilename = Path.Combine(_settings.DirectoryVideoAutoSave, GetRandomFileName("mp4", prefix));
                _logger?.LogInformation($"[AutoSaveVideoFile] Saving video, File: {videoFilename}");
                await videoResult.Video.SaveAsync(videoFilename);
                if (_settings.AutoSaveBlueprint)
                    await SaveBlueprintFileAsync(videoResult, videoFilename.Replace(".mp4", ".json"));

                _logger?.LogInformation($"[SaveVideoFile] Video file saved.");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[AutoSaveVideoFile] Error saving video: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="filename">The filename.</param>
        public async Task SaveVideoFile(VideoResultModel videoResult, string filename)
        {
            try
            {
                _logger?.LogInformation($"[SaveVideoFile] Saving video, File: {filename}");
                await videoResult.Video.SaveAsync(filename);
                _logger?.LogInformation($"[SaveVideoFile] Video file saved.");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveVideoFile] Error saving video: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the video file via FilePicker dialog.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        public async Task SaveAsVideoFile(VideoResultModel videoResult)
        {
            try
            {
                _logger?.LogInformation($"[SaveAsVideoFile] Saving video file...");
                var saveFileName = SaveVideoFileDialog($"{videoResult.SchedulerOptions.Seed}");
                if (string.IsNullOrEmpty(saveFileName))
                    return;

                await videoResult.Video.SaveAsync(saveFileName);
                _logger?.LogInformation($"[SaveAsVideoFile] Video file saved, File: {saveFileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveAsVideoFile] Error saving video: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves a temporary video file.
        /// </summary>
        /// <param name="videoBytes">The video bytes.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        public async Task<string> SaveTempVideoFile(OnnxVideo video, string prefix = default)
        {
            try
            {
                _logger?.LogInformation($"[SaveTempVideoFile] Saving temporary video file...");
                var tempVideoFile = GetTempFileName("mp4", prefix ?? "video");
                await video.SaveAsync(tempVideoFile);
                var videoBytes = await File.ReadAllBytesAsync(tempVideoFile);
                _logger?.LogInformation($"[SaveTempVideoFile] Temporary video file saved, File: {tempVideoFile}");
                return tempVideoFile;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveTempVideoFile] Error saving temporary video: {ex.Message}");
                return default;
            }
        }


        /// <summary>
        /// Deletes the temporary video file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <returns></returns>
        public Task DeleteTempVideoFile(VideoResultModel videoResult)
        {
            try
            {
                _logger?.LogInformation($"[DeleteTempVideoFile] Deleting temporary video file: {videoResult.FileName}");
                Task.Run(() =>
                {
                    File.Delete(videoResult.FileName);
                    _logger?.LogInformation($"[DeleteTempVideoFile] Temporary video file deleted.");
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[DeleteTempVideoFile] Error deleting temporary video: {ex.Message}");
            }
            return Task.CompletedTask;
        }


        /// <summary>
        /// Deletes the temporary video files.
        /// </summary>
        /// <param name="videoResults">The video results.</param>
        /// <returns></returns>
        public Task DeleteTempVideoFile(IEnumerable<VideoResultModel> videoResults)
        {
            Task.WhenAll(videoResults.Select(DeleteTempVideoFile));
            return Task.CompletedTask;
        }


        /// <summary>
        /// Opens the video file dialog.
        /// </summary>
        /// <returns></returns>
        private string OpenVideoFileDialog()
        {
            // Show Dialog
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open Video",
                Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.gif|Gif Images|*.gif|All Files|*.*",
                InitialDirectory = _settings.DirectoryVideo,
                RestoreDirectory = string.IsNullOrEmpty(_settings.DirectoryVideo),
                Multiselect = false,
            };
            if (openFileDialog.ShowDialog() != true)
            {
                _logger?.LogInformation("[OpenVideoFileDialog] Open video file canceled");
                return null;
            }

            return openFileDialog.FileName;
        }


        /// <summary>
        /// Saves the video file dialog.
        /// </summary>
        /// <param name="initialFilename">The initial filename.</param>
        /// <returns></returns>
        private string SaveVideoFileDialog(string initialFilename)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Video",
                Filter = "mp4 files (*.mp4)|*.mp4",
                DefaultExt = "mp4",
                AddExtension = true,
                RestoreDirectory = string.IsNullOrEmpty(_settings.DirectoryVideoSave),
                InitialDirectory = _settings.DirectoryVideoSave,
                FileName = $"video-{initialFilename}.mp4"
            };

            var dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult == false)
            {
                _logger?.LogInformation("[SaveVideoFileDialog] Saving video file canceled");
                return null;
            }
            return saveFileDialog.FileName;
        }

        #endregion

        #region Blueprint


        /// <summary>
        /// Saves the blueprint file dialog.
        /// </summary>
        /// <param name="initialFilename">The initial filename.</param>
        /// <param name="initialDirectory">The initial directory.</param>
        /// <returns></returns>
        private string SaveBlueprintFileDialog(string initialFilename, string initialDirectory)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Blueprint",
                Filter = "Json files (*.json)|*.json",
                DefaultExt = "json",
                AddExtension = true,
                RestoreDirectory = string.IsNullOrEmpty(initialDirectory),
                InitialDirectory = initialDirectory,
                FileName = $"{initialFilename}.json"
            };

            var dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult == false)
            {
                _logger?.LogInformation("[SaveBlueprintFileDialog] Saving Blueprint file canceled");
                return null;
            }
            return saveFileDialog.FileName;
        }


        /// <summary>
        /// Saves the blueprint file.
        /// </summary>
        /// <param name="imageResult"></param>
        /// <param name="filename">The filename.</param>
        public async Task SaveBlueprintFile(ImageResult imageResult, string filename)
        {
            try
            {
                _logger?.LogInformation($"[SaveBlueprintFile] Saving image Blueprint, File: {filename}");
                await SaveBlueprintFileAsync(imageResult, filename);
                _logger?.LogInformation($"[SaveBlueprintFile] Blueprint file saved.");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveBlueprintFile] Error saving Blueprint: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the blueprint file via FilePicker dialog.
        /// </summary>
        /// <param name="imageResult"></param>
        public async Task SaveAsBlueprintFile(ImageResult imageResult)
        {
            try
            {
                _logger?.LogInformation($"[SaveAsBlueprintFile] Saving image Blueprint file...");
                var saveFileName = SaveBlueprintFileDialog($"image-{imageResult.SchedulerOptions.Seed}", _settings.DirectoryImageSave);
                if (string.IsNullOrEmpty(saveFileName))
                    return;

                await SaveBlueprintFileAsync(imageResult, saveFileName);
                _logger?.LogInformation($"[SaveAsBlueprintFile] Blueprint file saved, File: {saveFileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveAsBlueprintFile] Error saving Blueprint: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the blueprint file.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="filename">The filename.</param>
        public async Task SaveBlueprintFile(VideoResultModel videoResult, string filename)
        {
            try
            {
                _logger?.LogInformation($"[SaveBlueprintFile] Saving video Blueprint, File: {filename}");
                await SaveBlueprintFileAsync(videoResult, filename);
                _logger?.LogInformation($"[SaveBlueprintFile] Blueprint file saved.");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveBlueprintFile] Error saving Blueprint: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the blueprint file via FilePicker dialog.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        public async Task SaveAsBlueprintFile(VideoResultModel videoResult)
        {
            try
            {
                _logger?.LogInformation($"[SaveAsBlueprintFile] Saving video Blueprint file...");
                var saveFileName = SaveBlueprintFileDialog($"video-{videoResult.SchedulerOptions.Seed}", _settings.DirectoryVideoSave);
                if (string.IsNullOrEmpty(saveFileName))
                    return;

                await SaveBlueprintFileAsync(videoResult, saveFileName);
                _logger?.LogInformation($"[SaveAsBlueprintFile] Blueprint file saved, File: {saveFileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[SaveAsBlueprintFile] Error saving Blueprint: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the blueprint file asynchronous.
        /// </summary>
        /// <param name="blueprint">The blueprint.</param>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        private static async Task<bool> SaveBlueprintFileAsync(IBlueprint blueprint, string filename)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fileStream, blueprint, serializerOptions);
                return File.Exists(filename);
            }
        }

        #endregion


        /// <summary>
        /// Deletes the temporary files.
        /// </summary>
        public async Task DeleteTempFiles()
        {
            try
            {
                await Task.WhenAll(Directory.EnumerateFiles(_settings.DirectoryTemp, "VideoToVideo-*")
               .Select(videoFile => Task.Run(() =>
               {
                   _logger?.LogInformation($"[DeleteTempFiles] Deleting temporary video file: {videoFile}");
                   File.Delete(videoFile);
                   _logger?.LogInformation($"[DeleteTempFiles] Temporary video file deleted.");
               })));
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[DeleteTempFiles] Error deleting temporary files: {ex.Message}");
            }
        }


        /// <summary>
        /// Gets a random filename
        /// </summary>
        /// <param name="ext">The ext.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        private string GetRandomFileName(string ext, string prefix = default)
        {
            return $"{prefix ?? "temp"}-{DateTime.Now.Ticks}.{ext}";
        }


        /// <summary>
        /// Gets a temporary filename.
        /// </summary>
        /// <param name="ext">The ext.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        private string GetTempFileName(string ext, string prefix = default)
        {
            if (!Directory.Exists(_settings.DirectoryTemp))
                Directory.CreateDirectory(_settings.DirectoryTemp);
            return Path.Combine(_settings.DirectoryTemp, GetRandomFileName(ext, prefix ?? "temp"));
        }
    }
}
