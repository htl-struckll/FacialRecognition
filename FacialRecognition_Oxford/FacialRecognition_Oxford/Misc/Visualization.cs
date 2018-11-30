using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using FaceAPI = Microsoft.ProjectOxford.Face.Contract;

namespace FacialRecognition_Oxford.Misc
{
    class Visualization
    {
        private static readonly SolidColorBrush Brush = new SolidColorBrush(Colors.Orange);
        private static readonly Typeface Typeface = new Typeface(new FontFamily("Verdana"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);


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

                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brush, lineThickness),
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

                        Rect rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(pen: null);
                        rect.Inflate(xpad, ypad);

                        drawingContext.DrawRectangle(Brush, null, rect);
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
