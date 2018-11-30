using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FacialRecognition_Oxford.Misc;
using Microsoft.ProjectOxford.Face.Contract;
using OpenCvSharp;

/*
 * todo rename _reader = _videoCapture
 * todo rename StartProcessingCameraAsync = StartProcessingAndGenerateCameraAsync
 */

namespace FacialRecognition_Oxford.VideoFrameAnalyzer
{
    class FrameGrabber
    {
        #region vars

        #region fields

        protected Predicate<VideoFrame> _analysisPredicate = null;
        VideoCapture _reader = null;
        Timer _timer = null;
        SemaphoreSlim _timerMutex = new SemaphoreSlim(1);
        AutoResetEvent _frameGrabTimer = new AutoResetEvent(false);
        bool _stopping = false;
        Task _producerTask = null;
        Task _consumerTask = null;
        BlockingCollection<Task<NewResultEventArgs>> _analysisTaskQueue = null;
        bool _resetTrigger = true;
        int _numCameras = -1;
        int _currCameraIdx = -1;
        double _fps = 0;

        #endregion

        #region Properties

        //VideoFrame = Parameter, Task<Face[]> = retVal
        public Func<VideoFrame, Task<LiveCameraResult>> AnalysisFunction { get; set; } = null;
        public TimeSpan AnalysisTimeout { get; set; } = TimeSpan.FromMilliseconds(5000);
        public bool IsRunning => _analysisTaskQueue != null;

        public double FrameRate
        {
            get => _fps;
            set
            {
                _fps = value;
                _timer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / _fps));
            }
        }

        public int Width { get; set; }
        public int Height { get; set; }

        #endregion

        #endregion


        public async Task StartProcessingCameraAsync(int cameraIndex = 0, double overrideFps = 30)
        {
            if (_reader == null && _reader.CaptureType != CaptureType.Camera && cameraIndex != _currCameraIdx) //check if we already have that camera
            {
                await StopProcessingAsync().ConfigureAwait(false);

                _reader = new VideoCapture(cameraIndex);
                _fps = overrideFps == 0 ? 30 : overrideFps;

                Width = _reader.FrameWidth;
                Height = _reader.FrameHeight;

                StartProcessing(TimeSpan.FromSeconds(1 / _fps));

                _currCameraIdx = cameraIndex;
            }
        }
    }
}