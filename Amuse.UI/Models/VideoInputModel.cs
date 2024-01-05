using OnnxStack.Core.Video;

namespace Amuse.UI.Models
{
    public class VideoInputModel
    {
        public VideoInfo VideoInfo { get; set; }
        public string FileName { get; set; }
        public byte[] VideoBytes { get; set; }
    }

}