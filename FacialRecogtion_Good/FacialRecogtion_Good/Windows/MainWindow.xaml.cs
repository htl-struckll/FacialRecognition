using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Rest;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

/*
 * Install-Package Microsoft.Azure.CognitiveServices.Vision.Face -Version 2.2.0-preview
 * https://portal.azure.com/#@htl-villach.at/resource/subscriptions/1671507b-42cd-42bf-ab70-9e0afef851b1/resourceGroups/Struckl_Container/providers/Microsoft.CognitiveServices/accounts/Struckl/quickstart
 */

namespace FacialRecogtion_Good.Windows
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region var
        #region api const

        private const string SubscriptionKey = "eafef65edfe04fb5b70a3f9739cc1618";

        private const string
            Endpoint =
                @"https://westeurope.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=true";

        #endregion
        private const string DefaultStatusBarText =
            "Place the mouse pointer over a face to see the face description.";
        private const string FilterPattern =
            @"Image files(*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

        private IList<DetectedFace> _faceList;
        private string[] _faceDescriptions;
        private double _resizeFactor;

        private readonly IFaceClient _faceClient = new FaceClient(
            new ApiKeyServiceClientCredentials(SubscriptionKey));
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _faceClient.Endpoint = Endpoint;
        }

        #region Events
        /// <summary>
        ///     Browse button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = Browse();
            if (!filePath.Equals(string.Empty))
            {
                Uri fileUri = new Uri(filePath);
                Analyze(fileUri, filePath);
            }
        }

        /// <summary>
        ///     Displays info in the infobox with a mouse over event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
            if (_faceList == null)
                return;

            Point mouseXy = e.GetPosition(FacePhoto);

            ImageSource imageSource = FacePhoto.Source;
            BitmapSource bitmapSource = (BitmapSource) imageSource;

            // Scale adjustment between the actual size and displayed size.
            double scale = FacePhoto.ActualWidth / (bitmapSource.PixelWidth / _resizeFactor);

            bool mouseOverFace = false;

            for (int i = 0; i < _faceList.Count; ++i)
            {
                FaceRectangle fr = _faceList[i].FaceRectangle;
                double left = fr.Left * scale;
                double top = fr.Top * scale;
                double width = fr.Width * scale;
                double height = fr.Height * scale;

                if (mouseXy.X >= left && mouseXy.X <= left + width &&
                    mouseXy.Y >= top && mouseXy.Y <= top + height)
                {
                    FaceDescriptionStatusBar.Text = _faceDescriptions[i];
                    mouseOverFace = true;
                    break;
                }
            }

            if (!mouseOverFace) FaceDescriptionStatusBar.Text = DefaultStatusBarText;
        }

        #endregion

        #region heart

        /// <summary>
        ///     Analyze the picture and load it into the programm
        /// </summary>
        /// <param name="fileUri"></param>
        /// <param name="filePath"></param>
        private async void Analyze(Uri fileUri, string filePath)
        {
            try
            {
                FacePhoto.Source = GetBitmapImage(fileUri);

                Title = "Detecting...";
                _faceList = await UploadAndDetectFaces(filePath);
                Title = $"Detection Finished. {_faceList.Count} face(s) detected";

                if (_faceList.Count > 0)
                    FacePhoto.Source = GeneratePictureWithRectangles(GetBitmapImage(fileUri));

                FaceDescriptionStatusBar.Text = DefaultStatusBarText;
            }
            catch (Exception ex)
            {
                ErrorOutput(ex.Message);
            }
        }

        /// <summary>
        ///     Uploads the image to the cloud and returns the detected faces with info
        /// </summary>
        /// <param name="imageFilePath">File path of image</param>
        /// <returns>IList of detected faces</returns>
        private async Task<IList<DetectedFace>> UploadAndDetectFaces(string imageFilePath)
        {
            IList<FaceAttributeType> faceAttributes =
                new[]
                {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Smile, FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses, FaceAttributeType.Hair
                };
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    HttpOperationResponse<IList<DetectedFace>> faceList =
                        await _faceClient.Face.DetectWithStreamWithHttpMessagesAsync(imageFileStream, true, true,
                            faceAttributes);
                    return faceList.Body;
                }
            }
            catch (APIErrorException apiErrorException)
            {
                ErrorOutput(apiErrorException.Message);
                return new List<DetectedFace>();
            }
            catch (Exception e)
            {
                ErrorOutput(e.Message);
                return new List<DetectedFace>();
            }
        }

        #endregion

        #region helpers

        /// <summary>
        ///     Gets the file from the user and converts it to a uri
        /// </summary>
        /// <returns></returns>
        private string Browse()
        {
            using (OpenFileDialog openDlg = new OpenFileDialog {Filter = FilterPattern})
            {
                if (openDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    return openDlg.FileName;
            }

            return string.Empty;
        }

        /// <summary>
        ///     Gets the bitmap image from the uri
        /// </summary>
        /// <param name="fileUri">Uri of the picture</param>
        /// <returns>Bitmap of the uri picture</returns>
        private BitmapImage GetBitmapImage(Uri fileUri)
        {
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            return bitmapSource;
        }

        /// <summary>
        ///     Generates rendered picture with rectangles around the faces
        /// </summary>
        /// <param name="bitmapSource">Bitmap source of person</param>
        /// <returns>rendertarget of the picture with rectangels</returns>
        private RenderTargetBitmap GeneratePictureWithRectangles(BitmapSource bitmapSource)
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();
            drawingContext.DrawImage(bitmapSource,
                new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
            double dpi = bitmapSource.DpiX;

            // Some images don't contain dpi info so we ´have to assume something (internet told so)
            _resizeFactor = dpi == 0 ? 1 : 96 / dpi;
            _faceDescriptions = new string[_faceList.Count];

            for (int i = 0; i < _faceList.Count; ++i)
            {
                DetectedFace face = _faceList[i];

                // Draw a rectangle on the face.
                drawingContext.DrawRectangle(
                    Brushes.Transparent,
                    new Pen(Brushes.Red, 2),
                    new Rect(
                        face.FaceRectangle.Left * _resizeFactor,
                        face.FaceRectangle.Top * _resizeFactor,
                        face.FaceRectangle.Width * _resizeFactor,
                        face.FaceRectangle.Height * _resizeFactor
                    )
                );

                _faceDescriptions[i] = GetFaceDescriptionString(face);
            }

            drawingContext.Close();

            // Display the image with the rectangle around the face.
            RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                (int) (bitmapSource.PixelWidth * _resizeFactor),
                (int) (bitmapSource.PixelHeight * _resizeFactor),
                96,
                96,
                PixelFormats.Pbgra32);

            faceWithRectBitmap.Render(visual);
            return faceWithRectBitmap;
        }

        /// <summary>
        ///     Get the face description of the detected
        /// </summary>
        /// <param name="face">Detected face</param>
        /// <returns>String of face description</returns>
        private string GetFaceDescriptionString(DetectedFace face)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Face: ");

            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append($"smile {face.FaceAttributes.Smile * 100:F1}%, ");

            // Add the emotions. Display all emotions over 50%.
            sb.Append("Emotion: ");
            Emotion emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.5f)
                sb.Append(
                    $"anger {emotionScores.Anger * 100:F1}%, ");
            if (emotionScores.Contempt >= 0.5f)
                sb.Append(
                    $"contempt {emotionScores.Contempt * 100:F1}%, ");
            if (emotionScores.Disgust >= 0.5f)
                sb.Append(
                    $"disgust {emotionScores.Disgust * 100:F1}%, ");
            if (emotionScores.Fear >= 0.5f)
                sb.Append(
                    $"fear {emotionScores.Fear * 100:F1}%, ");
            if (emotionScores.Happiness >= 0.5f)
                sb.Append(
                    $"happiness {emotionScores.Happiness * 100:F1}%, ");
            if (emotionScores.Neutral >= 0.5f)
                sb.Append(
                    $"neutral {emotionScores.Neutral * 100:F1}%, ");
            if (emotionScores.Sadness >= 0.5f)
                sb.Append(
                    $"sadness {emotionScores.Sadness * 100:F1}%, ");
            if (emotionScores.Surprise >= 0.5f)
                sb.Append(
                    $"surprise {emotionScores.Surprise * 100:F1}%, ");

            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");

            sb.Append("Hair: ");
            if (face.FaceAttributes.Hair.Bald >= 0.80f)
                sb.Append($"bald {face.FaceAttributes.Hair.Bald * 100:F1}% ");

            IList<HairColor> hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
                if (hairColor.Confidence >= 0.5f)
                {
                    sb.Append(hairColor.Color);
                    sb.Append($" {hairColor.Confidence * 100:F1}% ");
                }

            return sb.ToString();
        }

        #endregion

        #region output

        /// <summary>
        ///     Show pop up window to output
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
        ///     Show error popup window
        /// </summary>
        /// <param name="errMsg">Error message to display</param>
        private void ErrorOutput(string errMsg)
        {
            Output(errMsg, "Error", icn: MessageBoxImage.Error);
        }

        #endregion
    }
}