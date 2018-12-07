using EmotionScores = Microsoft.ProjectOxford.Common.Contract.EmotionScores;
using Face = Microsoft.ProjectOxford.Face.Contract.Face;

namespace FacialRecognition_Oxford.VideoFrameAnalyzer
{
    class LiveCameraResult
    {
        public Face[] Faces { get; set; } = null;
        public EmotionScores[] EmotionScores { get; set; } = null;

    }
}