using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FacialRecognition_Oxford.Misc;
using Microsoft.ProjectOxford.Common.Contract;
using FaceAPI = Microsoft.ProjectOxford.Face.Contract;

namespace FacialRecognition_Oxford.VideoFrameAnalyzer
{
    class Visualization
    {
        private static readonly SolidColorBrush MaleBrush = new SolidColorBrush(Colors.DeepSkyBlue);
        private static readonly SolidColorBrush FemaleBrush = new SolidColorBrush(Colors.Pink);
        private static readonly Typeface Typeface = new Typeface(new FontFamily("Verdana"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        /// <summary>
        /// Renders the rectangles
        /// </summary>
        /// <param name="baseImage">Base image</param>
        /// <param name="faces">The faces to draw rectangle</param>
        /// <param name="emotionScores">The emotion scores</param>
        /// <returns>Rendered bitmapsource</returns>
        public static BitmapSource DrawOverlay(BitmapSource baseImage, FaceAPI.Face[] faces, EmotionScores[] emotionScores)
        {
           double annotationScale = baseImage.PixelHeight / 320;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();

            drawingContext.DrawImage(baseImage, new Rect(0, 0, baseImage.Width, baseImage.Height));

            for (int i = 0; i < faces.Length; i++)
            {
                FaceAPI.Face face = faces[i];

                if (face.FaceRectangle != null)
                {
                    double lineThickness = 4 * annotationScale;

                    Rect faceRect = new Rect(
                        face.FaceRectangle.Left, face.FaceRectangle.Top,
                        face.FaceRectangle.Width, face.FaceRectangle.Height);
                    string text = string.Empty;

                    if (face.FaceAttributes != null)
                        text += Helper.GetFaceAttributesAsString(face.FaceAttributes);
                    if (emotionScores?[i] != null)
                        text += ", " + Helper.GetDominantEmotionAsString(emotionScores[i]);

                    faceRect.Inflate(6 * annotationScale, 6 * annotationScale);

                    SolidColorBrush genderBrush = face.FaceAttributes.Gender.ToLower().Equals("male")
                        ? MaleBrush
                        : FemaleBrush;

                        drawingContext.DrawRectangle(
                            Brushes.Transparent,
                            new Pen(genderBrush, lineThickness),
                            faceRect);
                    
                    //Generate rectangle background for text and place text in it
                    if (text != string.Empty)
                    {
                        FormattedText ft = new FormattedText(text,
                            CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface,
                            16 * annotationScale, Brushes.Black);

                        double pad = 3 * annotationScale;
                        double ypad = pad;
                        double xpad = pad + 4 * annotationScale;

                        Point origin = new Point(
                            faceRect.Left + xpad - lineThickness / 2,
                            faceRect.Top - ft.Height - ypad + lineThickness / 2);

                        Rect rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                        rect.Inflate(xpad, ypad);

                        drawingContext.DrawRectangle(genderBrush, null, rect);
                        drawingContext.DrawText(ft, origin);
                    }
                }
            }

            drawingContext.Close();

            RenderTargetBitmap outputBitmap = new RenderTargetBitmap(
                baseImage.PixelWidth, baseImage.PixelHeight,
                baseImage.DpiX, baseImage.DpiY, PixelFormats.Pbgra32);

            outputBitmap.Render(visual);

            return outputBitmap;
        }
    }
}
