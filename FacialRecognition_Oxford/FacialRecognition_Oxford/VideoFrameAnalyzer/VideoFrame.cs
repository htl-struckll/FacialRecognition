using System;
using OpenCvSharp;

namespace FacialRecognition_Oxford.VideoFrameAnalyzer
{
    struct VideoFrameMetadata
    {
        public DateTime TimeStamp;
    }

    class VideoFrame
    {
        public Mat Image { get; set; }
        public Rect[] Rectangles { get; set; }
        public VideoFrameMetadata VideoFrameMetadata { get; set; }

        public VideoFrame(VideoFrameMetadata metadata, Mat image)
        {
            VideoFrameMetadata = metadata;
            Image = image;
        }
    }
}
