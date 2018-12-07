using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FacialRecognition_Oxford.Data;
using FacialRecognition_Oxford.Events;
using FacialRecognition_Oxford.Misc;
using OpenCvSharp;

namespace FacialRecognition_Oxford.VideoFrameAnalyzer
{
    class FrameGrabber
    {
        #region vars

        #region fields

        /// <summary>
        /// Camera task
        /// </summary>
        private Task _producerTask = null;

        /// <summary>
        /// Analyzer task
        /// </summary>
        private Task _consumerTask = null;

        /// <summary>
        /// If the frame should be analyzed
        /// </summary>
        protected Predicate<VideoFrame> ShouldAnalyze;

        private VideoCapture _videoCapture;
        private Timer _timer;
        private readonly SemaphoreSlim _timerMutex = new SemaphoreSlim(1);
        private readonly AutoResetEvent _frameGrabTimer = new AutoResetEvent(false);
        private bool _stopping;
        private BlockingCollection<Task<NewResultEventArgs>> _analysisTaskQueue;
        private bool _isFirstFrame = true;
        private int _numCameras = -1;
        private int _currCameraIdx = -1;
        private double _fps;

        #endregion fields

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

        #endregion Properties

        #region Events

        public event EventHandler ProcessingStarting;
        public event EventHandler ProcessingStarted;
        public event EventHandler ProcessingStopping;
        public event EventHandler ProcessingStopped;
        public event EventHandler<NewFrameEventArgs> NewFrameProvided;
        public event EventHandler<NewResultEventArgs> NewResultAvailable;

        #endregion Events

        #endregion vars

        #region Methods

        /// <summary>
        /// Initializes the camera and starts it 
        /// </summary>
        /// <param name="cameraIndex">Camera index</param>
        /// <param name="overrideFps">Fps to record</param>
        /// <returns></returns>
        public async Task StartProcessingAndGenerateCameraAsync(int cameraIndex = 0, double overrideFps = 30)
        {
            if (_videoCapture == null || _videoCapture.CaptureType != CaptureType.Camera ||
                cameraIndex != _currCameraIdx)
            {
                await StopProcessingAsync().ConfigureAwait(false);

                _videoCapture = new VideoCapture(cameraIndex);
                _fps = overrideFps == 0 ? 30 : overrideFps;

                Width = _videoCapture.FrameWidth;
                Height = _videoCapture.FrameHeight;
                
                StartProcessing(TimeSpan.FromSeconds(1 / _fps));

                _currCameraIdx = cameraIndex;
            }
        }


        /// <summary>
        /// Generates producer (camera) and consumer (analyzer)
        /// </summary>
        /// <param name="frameGrabDelay">Delay to grab the frame</param>
        protected void StartProcessing(TimeSpan frameGrabDelay)
        {
            OnProcessingStarting();

            _isFirstFrame = true;
            _frameGrabTimer.Reset();
            _analysisTaskQueue = new BlockingCollection<Task<NewResultEventArgs>>();

            // Create a background thread that will grab frames in a loop.
            _producerTask = Task.Factory.StartNew(async () =>
            {
                while (!_stopping)
                {
                    // Wait to get released by the timer.
                    _frameGrabTimer.WaitOne();

                    // Grab single frame.
                    Mat image = new Mat();
                    bool success = _videoCapture.Read(image);

                    if (!success)
                    {
                        // If we've reached the end of the video, stop here.
                        if (_videoCapture.CaptureType == CaptureType.File)
                        {
                            await StopProcessingAsync();
                            break;
                        }
                        continue;
                    }

                    VideoFrame vframe = new VideoFrame(new VideoFrameMetadata(){TimeStamp = DateTime.Now}, image);

                    // Raise the new frame event
                    OnNewFrameProvided(vframe);

                    if (ShouldAnalyze(vframe))
                    {
                        var analysisTask = DoAnalyzeFrame(vframe);
                        _analysisTaskQueue.Add(analysisTask);
                    }

                }

                _analysisTaskQueue.CompleteAdding();

                _videoCapture.Dispose();
                _videoCapture = null;

                // Make sure the timer stops, then get rid of it.
                ManualResetEvent tmpReset = new ManualResetEvent(false);
                _timer.Dispose(tmpReset);
                tmpReset.WaitOne();
                _timer = null;

            }, TaskCreationOptions.LongRunning);

            _consumerTask = Task.Factory.StartNew(async () =>
            {
                while (!_analysisTaskQueue.IsCompleted)
                {

                    // Get the next processing task.
                    Task<NewResultEventArgs> nextTask = null;

                    //Makes the producer be able to call 'CompleteAdding()'
                    try
                    {
                        nextTask = _analysisTaskQueue.Take();
                    }
                    catch (InvalidOperationException) { Helper.ConsoleLog("oof"); } 

                    if (nextTask != null)
                        OnNewResultAvailable(await nextTask);
                }

            }, TaskCreationOptions.LongRunning);

            // Set up a timer object that will trigger the frame-grab at interval.
            _timer = new Timer(async state  =>
            {
                await _timerMutex.WaitAsync();
                try
                {
                    // If the handle was not reset by the producer, then the frame-grab was missed.
                    _frameGrabTimer.WaitOne(0);
                    _frameGrabTimer.Set();
                }
                finally
                {
                    _timerMutex.Release();
                }
            }, null, TimeSpan.Zero, frameGrabDelay);

            OnProcessingStarted();
        }

        /// <summary>
        /// Stops the processing, Setting all on null 
        /// </summary>
        /// <returns></returns>
        public async Task StopProcessingAsync()
        {
            OnProcessingStopping();

            _stopping = true;
            _frameGrabTimer.Set();

            if (_producerTask != null)
            {
                await _producerTask;
                _producerTask = null;
            }

            if (_consumerTask != null)
            {
                await _consumerTask;
                _consumerTask = null;
            }

            if (_analysisTaskQueue != null)
            {
                _analysisTaskQueue.Dispose();
                _analysisTaskQueue = null;
            }
            _stopping = false;

            OnProcessingStopped();
        }

        /// <summary>
        /// Generates the function for <see cref="ShouldAnalyze"/> to check if the new image is the newest image
        /// </summary>
        /// <param name="interval">Interval of getting a new frame</param>
        public void TriggerAnalysisOnInterval(TimeSpan interval)
        {
            _isFirstFrame = true;

            // Keep track of the next timestamp to trigger. (Only minVal on first run, Don´t get confused future Struckl)
            DateTime nextCall = DateTime.MinValue;
            ShouldAnalyze = frame =>
            {
                bool shouldCall = false;

                // If this is the first frame, then trigger and initialize the timer. 
                if (_isFirstFrame)
                {
                    _isFirstFrame = false;
                    nextCall = frame.VideoFrameMetadata.TimeStamp;
                    shouldCall = true;
                }
                else
                {
                    shouldCall = frame.VideoFrameMetadata.TimeStamp > nextCall;
                }

                if (shouldCall)
                {
                    nextCall += interval;
                    return true;
                }
                return false;
            };
        }

        /// <summary>
        /// Gets the cameras
        /// </summary>
        /// <returns>The nunmber of cameras</returns>
        public int GetNumCameras()
        {
            if (_numCameras == -1)
            {
                _numCameras = 0;
                while (_numCameras < 100)
                {
                    using (VideoCapture vc = VideoCapture.FromCamera(_numCameras))
                    {
                        if (vc.IsOpened())
                            ++_numCameras;
                        else
                            break;
                    }
                }
            }

            return _numCameras;
        }

        /// <summary>
        /// Analyze the frame
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        protected async Task<NewResultEventArgs> DoAnalyzeFrame(VideoFrame frame)
        {
            CancellationTokenSource source = new CancellationTokenSource();

            // Make a local reference to the function, just I set it to null before I call it couse i am stupid
            Func<VideoFrame, Task<LiveCameraResult>> functionAnalyzisFunction = AnalysisFunction;
            if (functionAnalyzisFunction != null)
            {
                NewResultEventArgs output = new NewResultEventArgs(frame);
                Task<LiveCameraResult> task = functionAnalyzisFunction(frame);
                try
                {
                    if (task == await Task.WhenAny(task, Task.Delay(AnalysisTimeout, source.Token)))
                    {
                        output.Analysis = await task;
                        source.Cancel();
                    }
                    else
                        output.TimedOut = true;
                }
                catch (Exception ae)
                {
                    output.Exception = ae;
                }

                return output;
            }

            return null;
        }

        #region EventTriggers
        /// <summary> Triggers ProcessingStarting event </summary>
        protected void OnProcessingStarting() => ProcessingStarting?.Invoke(this, null);
        
        /// <summary> Triggers ProcessingStarted event </summary>
        protected void OnProcessingStarted() => ProcessingStarted?.Invoke(this, null);

        /// <summary> Triggers the ProcessingStopping event </summary>
        protected void OnProcessingStopping() => ProcessingStopping?.Invoke(this, null);
        
        /// <summary>  Triggers ProcessingStopped  </summary>
        protected void OnProcessingStopped() => ProcessingStopped?.Invoke(this, null);

        /// <summary>
        /// Triggers the NewFrameProvided event
        /// </summary>
        /// <param name="frame">Frame which is given to the event</param>
        protected void OnNewFrameProvided(VideoFrame frame) => NewFrameProvided?.Invoke(this, new NewFrameEventArgs(frame));

        /// <summary>
        /// Triggers the NewResultAvailable event
        /// </summary>
        /// <param name="args"></param>
        protected void OnNewResultAvailable(NewResultEventArgs args) => NewResultAvailable?.Invoke(this, args);

        #endregion  EventTriggers

        #endregion Methods
    }
}