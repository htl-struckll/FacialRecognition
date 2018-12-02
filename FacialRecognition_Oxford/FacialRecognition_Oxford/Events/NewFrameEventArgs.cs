using System;
using FacialRecognition_Oxford.VideoFrameAnalyzer;

namespace FacialRecognition_Oxford.Events
{
    /// <summary> NewFrameEvent </summary>
    class NewFrameEventArgs : EventArgs
    {
        public NewFrameEventArgs(VideoFrame frame)
        {
            Frame = frame;
        }
        public VideoFrame Frame { get; }
    }
}
