using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using Emgu.CV;
using Emgu.CV.Structure;
using Size = System.Drawing.Size;
using SRectangle = System.Windows.Shapes.Rectangle;

/*
 * bad at recognizing
 * no resize factor
 * just shit
 */

namespace FacialRecognition_Emgu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region vars
        private readonly VideoCapture _capture;
        private readonly Mat _frame;

        private CascadeClassifier _cascadeClassifier;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _capture = new VideoCapture(0);
            _frame = new Mat();

            _cascadeClassifier = new CascadeClassifier(AppDomain.CurrentDomain.BaseDirectory +  "haarcascade_frontalface_alt_tree.xml");
            _capture.ImageGrabbed += ProcessFrame;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _capture.Start();
        }

        /// <summary>
        /// Displays frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ProcessFrame(object sender, EventArgs e)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);

                DisplayRawImage.Dispatcher.Invoke(
                    () => DisplayRawImage.Source = BitmapToImageSource(_frame.Bitmap));

                Image<Gray, byte> grayframe = _frame.ToImage<Gray, byte>();
                Rectangle[] faces = _cascadeClassifier.DetectMultiScale(grayframe, 1.1, 10, Size.Empty);

                DisplayRectangleCanvas.Dispatcher.Invoke(() => DisplayRectangleCanvas.Children.Clear());

                foreach (Rectangle face in faces)
                {
                    DisplayRectangleCanvas.Dispatcher.Invoke(() =>
                    {
                        SRectangle rct = new SRectangle();
                        Canvas.SetLeft(rct, face.Left );
                        Canvas.SetTop(rct, face.Top );
                        rct.Width = face.Width ;
                        rct.Height = face.Height ;
                        rct.Stroke = new SolidColorBrush(Colors.AliceBlue);
                        DisplayRectangleCanvas.Children.Add(rct);
                    });
                }
            }
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
        /// Console log function
        /// </summary>
        /// <param name="msg"></param>
        private void ConsoleLog(string msg) => Console.WriteLine("[" + DateTime.Now + "] " + msg);
    }
}
