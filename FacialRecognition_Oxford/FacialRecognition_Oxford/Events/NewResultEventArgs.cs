using System;
using FacialRecognition_Oxford.Camera;
using FacialRecognition_Oxford.VideoFrameAnalyzer;

namespace FacialRecognition_Oxford.Events
{
    /// <summary> NewResultEvent </summary>
    class NewResultEventArgs : EventArgs
    {
        public NewResultEventArgs(VideoFrame frame)
        {
            Frame = frame;
        }

        public VideoFrame Frame { get; }
        public LiveCameraResult Analysis { get; set; } = default(LiveCameraResult);
        public bool TimedOut { get; set; } = false;
        public Exception Exception { get; set; } = null;
    }
}
