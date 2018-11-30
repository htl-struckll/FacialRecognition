using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Rest;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Person = FacialRecognition_Azure.Data.Person;
using Thread = System.Threading.Thread;

/*
 * Install-Package Microsoft.Azure.CognitiveServices.Vision.Face -Version 2.2.0-preview
 */

namespace FacialRecognition_Azure.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region var

        #region const

        private const string SubscriptionKey = "eafef65edfe04fb5b70a3f9739cc1618";
        private const string ErrorFilePath = @"ProgrammFiles\error.log";

        #endregion

        private IList<DetectedFace> _faceList;
        //private string[] _faceDescriptions;
        private double _resizeFactor;

        private readonly VideoCapture _capture;
        private readonly Mat _frame;
        private const uint SkpFrames = 4; //to send the allowed amount of requests
        private int _frameCnt;
        private bool _recording;
        private Bitmap _imageBitmap;
        private readonly DispatcherTimer _timer;
        //private List<Thread> _threads;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _capture = new VideoCapture(0);
            _frame = new Mat();

            //_threads = new List<Thread>();

            _timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 10)
            };
            //_timer.Tick += DispatchTimerTick;

            _faceList = new List<DetectedFace>();

            _capture.ImageGrabbed += CaptureDeviceGetsFrame;
        }
       

        #region Heart

        /// <summary>
        /// Starts recording
        /// </summary>
        private void StartRecording()
        {
            _capture.Start();
            _timer.Start();
        } 

        /// <summary>
        /// Stops recording
        /// </summary>
        private void StopRecording()
        {
            _capture.Stop();
            _timer.Stop();
        } 

        /// <summary>
        /// Uploading and rendering the picture
        /// </summary>
        private void RenderAndProcessFrame()
        {
            _capture.Retrieve(_frame, 0);
            _imageBitmap = _frame.Bitmap;

            Thread uploadThread = new Thread(UploadAndDetectFaces);
            uploadThread.Start();
            //_threads.Add(tmp);
            //_threads[_threads.Count - 1].Start();

            BitmapSource source = BitmapToBitmapSource(_frame.Bitmap);
            RenderTargetBitmap withRectangle = GeneratePictureWithRectangles(source);
            Bitmap renderTargetBitmap = RenderTargetBitmapToBitmap(withRectangle);


            _frameCnt = 0;
            CamDisplayRaw.Dispatcher.Invoke(
                () => CamDisplayRaw.Source = BitmapToImageSource(renderTargetBitmap));
        }

        /// <summary>
        /// Uploads the image to the cloud and returns the detected faces with info
        /// </summary>
        private async void  UploadAndDetectFaces()
        {
            IFaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SubscriptionKey));
            faceClient.Endpoint =
                @"https://westeurope.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=true";

            IList<FaceAttributeType> faceAttributes =
                new[]
                {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Emotion, FaceAttributeType.Glasses,
                    FaceAttributeType.Hair
                };
            try
            {
                Stream stream = ImageToStream(Image.FromHbitmap(_imageBitmap.GetHbitmap()), ImageFormat.Jpeg);
                HttpOperationResponse<IList<DetectedFace>> faceList =
                    await faceClient.Face.DetectWithStreamWithHttpMessagesAsync(stream, true, true, faceAttributes);

                _faceList = faceList.Body;
            }
            catch (APIErrorException apiErrorException)
            {
                ErrorOutput(apiErrorException);
            }
            catch (Exception e)
            {
                ErrorOutput(e);
            }
        }

        /// <summary>
        /// Generates rendered picture with rectangles around the faces
        /// </summary>
        /// <param name="bitmapSource">Bitmap source of person</param>
        /// <returns>rendertarget of the picture with rectangels</returns>
        private RenderTargetBitmap GeneratePictureWithRectangles(BitmapSource bitmapSource)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen()
            ) 
            {
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;

                // Some images don't contain dpi info so we ´have to assume something (internet told so)
                _resizeFactor = dpi == 0 ? 1 : 96 / dpi;

                foreach (DetectedFace face in _faceList)
                {
                    Person tmpPerson = new Person(face);
                    FormattedText formattedText = new FormattedText(
                        tmpPerson.Emotion + ", " + tmpPerson.Gender,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        20,
                        Brushes.Red);

                    drawingContext.DrawEllipse(
                        Brushes.Transparent,
                            new Pen(Brushes.Red, 2),
                            new Point(face.FaceRectangle.Left * _resizeFactor + (face.FaceRectangle.Width * _resizeFactor) / 2,(face.FaceRectangle.Height*_resizeFactor) / 3 + face.FaceRectangle.Top * _resizeFactor)
                        ,face.FaceRectangle.Width,
                        face.FaceRectangle.Height
                        );

                    //drawingContext.DrawRectangle(
                    //    Brushes.Transparent,
                    //    new Pen(Brushes.Red, 2),
                    //    new Rect(
                    //        face.FaceRectangle.Left * _resizeFactor,
                    //        face.FaceRectangle.Top * _resizeFactor,
                    //        face.FaceRectangle.Width * _resizeFactor,
                    //        face.FaceRectangle.Height * _resizeFactor
                    //    )
                    //);

                    //10,30
                    Point p = new Point((face.FaceRectangle.Left - 50) * _resizeFactor,
                        (face.FaceRectangle.Top - 70) * _resizeFactor);

                    drawingContext.DrawText(formattedText, p);
                }

                drawingContext.Close();
            }

            // Render the image
            RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                (int)(bitmapSource.PixelWidth * _resizeFactor),
                (int)(bitmapSource.PixelHeight * _resizeFactor),
                96,
                96,
                PixelFormats.Pbgra32);

            faceWithRectBitmap.Render(visual);

            return faceWithRectBitmap;
        }

        /// <summary>
        /// Clearing the thread list
        /// </summary>
        private void ClearThreadList()
        {
            //_threads.ForEach(thread => thread.Abort());
                
            // ConsoleLog(_threads.Count + " threads cleared");
            //_threads.Clear();
            ConsoleLog("No thread list to clear");
        }

        #endregion

        #region output

        /// <summary>
        /// Show pop up window to output
        /// </summary>
        /// <param name="msg">Message to display</param>
        /// <param name="caption">Caption to display</param>
        /// <param name="btn">Button to press</param>
        /// <param name="icn">Icon to display</param>
        private void Output(string msg, string caption = "Info", MessageBoxButton btn = MessageBoxButton.OK,
            MessageBoxImage icn = MessageBoxImage.Information)
        {
            MessageBox.Show(msg, caption, btn, icn);
        }

        /// <summary>
        /// Show error popup window
        /// </summary>
        /// <param name="ex">Exception to display</param>
        private void ErrorOutput(Exception ex)
        {
            if (ex is OutOfMemoryException)
                Output("ATTENTION! This is a out of Memory exception. Please seak shelter in the nearest Stackoverflow forum!\n" + ex.Message + "|" + ex.StackTrace, "Error", icn: MessageBoxImage.Error);
            else
                Output(ex.Message + "|" + ex.StackTrace, "Error", icn: MessageBoxImage.Error);
        }

        /// <summary>
        /// Logs msg to console
        /// </summary>
        /// <param name="msg"></param>
        public static void ConsoleLog(string msg) => Console.WriteLine("[" + DateTime.Now + "] " + msg);

        #endregion

        #region events
        /// <summary>
        /// Clears the thread list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearThreads_Click(object sender, RoutedEventArgs e)
        {
            ClearThreadList();
        }

        /// <summary>
        /// Displays frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureDeviceGetsFrame(object sender, EventArgs e)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _frameCnt++;

                if (_frameCnt % SkpFrames == 0)
                    RenderAndProcessFrame();
            }
        }

        /// <summary>
        /// Starts/stops the recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            StartStopBtn.Header = _recording ? "Start" : "Stop";

            if (_recording)
            {
                _recording = false;
                StopRecording();
            }
            else
            {
                _recording = true;
                StartRecording();
            }
        }

        /// <summary>
        /// Dispatch timer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispatchTimerTick(object sender, EventArgs e)
        {
            ClearThreadList();
        }

        /// <summary>
        /// Display the data window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayData_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("not done, went on with emgu");
        }

        #endregion

        #region Converter

        /// <summary>
        ///     Convert image to stream
        /// </summary>
        /// <param name="image"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private Stream ImageToStream(Image image, ImageFormat format)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Converts a bitmap image to a image source
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        /// <summary>
        /// Convert bitmap to bitmapsource
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            BitmapSource bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        /// <summary>
        /// Convert bitmapimage to bitmap
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public Bitmap BitmapImageToBitmap(BitmapImage image)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(image));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        /// <summary>
        /// Convert RenderTargetbitmap to bitmap
        /// </summary>
        /// <param name="renderTargetBitmap"></param>
        /// <returns></returns>
        public Bitmap RenderTargetBitmapToBitmap(RenderTargetBitmap renderTargetBitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            PngBitmapEncoder bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using (MemoryStream stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }

            return BitmapImageToBitmap(bitmapImage);
        }

        #endregion

        #region LocalSaves

        /// <summary>
        ///     Saves the error msg to file
        /// </summary>
        /// <param name="ex">Exception to save</param>
        private void SaveErrorMsg(Exception ex)
        {
            using (StreamWriter writer = new StreamWriter(ErrorFilePath, true))
            {
                writer.WriteLine("[" + DateTime.Now + "] " + ex.Message + " | " + ex.StackTrace);
            }
        }

        #endregion


    }
}
