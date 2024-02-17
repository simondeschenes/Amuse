using OnnxStack.Core.Video;

namespace Amuse.UI.Models
{
    public class VideoInputModel
    {
        public string FileName { get; set; }
        public OnnxVideo Video { get; set; }
    }
}