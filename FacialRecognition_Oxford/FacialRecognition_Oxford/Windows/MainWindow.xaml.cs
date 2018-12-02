using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using FacialRecognition_Oxford.Camera;
using FacialRecognition_Oxford.Misc;
using FacialRecognition_Oxford.VideoFrameAnalyzer;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using OpenCvSharp.Extensions;
using FaceAPI = Microsoft.ProjectOxford.Face;
using Rect = OpenCvSharp.Rect;


namespace FacialRecognition_Oxford.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region vars

        #region const

        private const string SubscriptionKey = "eafef65edfe04fb5b70a3f9739cc1618";
        private const string EnpointUri = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";

        #endregion const

        private readonly FaceAPI.FaceServiceClient _faceClient;
        private readonly FrameGrabber _grabber;

        private readonly ImageEncodingParam[] JpegParams =
        {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };

        private readonly CascadeClassifier _localFaceDetector;
        private bool _isFuseClientRemoteResults;
        private LiveCameraResult _latestResultsToDisplay;
        private readonly List<Guid> _facesGuids;
        #endregion vars

        public MainWindow()
        {
            InitializeComponent();

            _faceClient = null;
            _grabber = new FrameGrabber();
            _localFaceDetector = new CascadeClassifier();
            _latestResultsToDisplay = null;
            _faceClient = new FaceAPI.FaceServiceClient(SubscriptionKey, EnpointUri);
            _facesGuids = new List<Guid>();

            InitEvents();

            // Create local face detector. 
            _localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");
        }

        #region methods

        /// <summary>
        /// Inits all events
        /// </summary>
        private void InitEvents()
        {
            //new frame
            _grabber.NewFrameProvided += (s, e) =>
            {
                e.Frame.Rectangles = _localFaceDetector.DetectMultiScale(e.Frame.Image);

                Dispatcher.BeginInvoke((Action) (() =>
                {
                    if (_isFuseClientRemoteResults)
                    {
                        DisplayImage.Source = VisualizeResult(e.Frame);
                    }
                }));
            };

            // recive result from api 
            _grabber.NewResultAvailable += (s, e) =>
            {
                Dispatcher.BeginInvoke((Action) (() =>
                {
                    if (e.TimedOut)
                        MessageArea.Text = "API call timed out.";
                    else if (e.Exception is FaceAPI.FaceAPIException)
                        MessageArea.Text =
                            $"Face API call failed on frame {e.Frame.VideoFrameMetadata.TimeStamp}. Exception: " +
                            e.Exception.Message;
                    else
                    {
                        _latestResultsToDisplay = e.Analysis;

                        // Display the image and visualization in the right pane. 
                        if (!_isFuseClientRemoteResults)
                        {
                            DisplayImage.Source = VisualizeResult(e.Frame);
                        }
                    }
                }));
            };
        }

        /// <summary>
        /// Generate attributes to get from api and uploads it
        /// </summary>
        /// <param name="frame">Current picture</param>
        /// <returns>Analyced LiveCameraResult</returns>
        private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
        {
            MemoryStream jpg = frame.Image.ToMemoryStream(".jpg", JpegParams);

            List<FaceAPI.FaceAttributeType> attrs = new List<FaceAPI.FaceAttributeType>
            {
                FaceAPI.FaceAttributeType.Age,
                FaceAPI.FaceAttributeType.Gender,
                FaceAPI.FaceAttributeType.Emotion
            };

            Face[] faces = await _faceClient.DetectAsync(jpg, returnFaceAttributes: attrs, returnFaceLandmarks: true);
            EmotionScores[] scores = faces.Select(e => e.FaceAttributes.Emotion).ToArray();

            foreach (var face in faces)
            {
                if (!await CheckIfFaceWasSeenBefore(face.FaceId))
                {                    //todo call update in displayWindow with new data
                    _facesGuids.Add(face.FaceId);
                    Helper.ConsoleLog(face.FaceId + " is new!" + _facesGuids.Count);
                }
            }

            return new LiveCameraResult { Faces = faces, EmotionScores = scores };
        }


        /// <summary>
        /// Checks if the person has been seen before 
        /// </summary>
        /// <param name="fId"></param>
        /// <returns></returns>
        private async Task<bool> CheckIfFaceWasSeenBefore(Guid fId)
        {
            if (_facesGuids.Count == 0)
                return false;

            bool retVal = false;
            SimilarFace[] val = await _faceClient.FindSimilarAsync(fId, _facesGuids.ToArray());

            foreach (var similarFace in val)
            {
                if (similarFace.Confidence > 0.5)
                    retVal = true;
            }

            return retVal;
        }
        /// <summary>
        /// Fuses the api and local detection and renders the new picture
        /// </summary>
        /// <param name="frame">Frame to do so</param>
        /// <returns>New bitmapsource where stuff has been done</returns>
        private BitmapSource VisualizeResult(VideoFrame frame)
        {
            BitmapSource visImage = frame.Image.ToBitmapSource();
            LiveCameraResult result = _latestResultsToDisplay;

            if (result != null)
            {
                Rect[] clientFaces = frame.Rectangles;
                if (clientFaces != null && result.Faces != null)
                    MatchAndReplaceFaceRectangles(result.Faces, clientFaces);

                visImage = Visualization.DrawOverlay(visImage, result.Faces, result.EmotionScores);
            }

            return visImage;
        }

        /// <summary>
        /// Matches the Faces and Rectangles by sorting them from left to right and assuming 1:1 correspondence
        /// </summary>
        /// <param name="faces">Faces</param>
        /// <param name="clientRects">Rectangles</param>
        private void MatchAndReplaceFaceRectangles(Face[] faces, Rect[] clientRects)
        {
            Face[] sortedResultFaces = faces
                .OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
                .ToArray();

            Rect[] sortedClientRects = clientRects
                .OrderBy(r => r.Left + 0.5 * r.Width)
                .ToArray();

            for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++)
            {
                Rect r = sortedClientRects[i];
                sortedResultFaces[i].FaceRectangle =
                    new FaceRectangle {Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height};
            }
        }

        #endregion methods  

        #region events

        /// <summary>
        /// Start/stop the programm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem) sender).Header.Equals("Start"))
            {
                _grabber.AnalysisFunction = FacesAnalysisFunction;
                _isFuseClientRemoteResults = true;

                int freq = Convert.ToInt32(UploadFrequencySlider.Value * 1000);

                _grabber.TriggerAnalysisOnInterval(new TimeSpan(0, 0, 0, 0, freq));

                MessageArea.Text = "";
                ((MenuItem) sender).Header = "Stop";

                await _grabber.StartProcessingAndGenerateCameraAsync(CameraList.SelectedIndex);
            }
            else
            {
                ((MenuItem) sender).Header = "Start";
                await _grabber.StopProcessingAsync();
            }
        }


        private void DisplayData_Click(object sender, RoutedEventArgs e)
        {
            Helper.WindowLog("Not implemented yet", icon: MessageBoxImage.Error);
        }

        /// <summary>
        /// Loads the cameras in a combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraList_Loaded(object sender, RoutedEventArgs e)
        {
            int numCameras = _grabber.GetNumCameras();

            if (numCameras == 0)
            {
                MessageArea.Text = "No cameras found!";
            }

            ComboBox comboBox = sender as ComboBox;
            comboBox.ItemsSource = Enumerable.Range(0, numCameras).Select(i => $"Camera {i + 1}");
            comboBox.SelectedIndex = 0;
        }

        #endregion events

    }
}