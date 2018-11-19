using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

//todo
namespace FacialDetection_Emgu.Data
{
    class DisplayData
    {
        public uint Persons { get; set; }
        public uint Male { get; set; }
        public uint Female { get; set; }
        public double PercentFemale { get; set; }
        public double PercentMale { get; set; }
        public double PercentTeens { get; set; }
        public double PercentAdults { get; set; }

        public IList<DetectedFace> FaceList { get; set; }

        
    }
}
