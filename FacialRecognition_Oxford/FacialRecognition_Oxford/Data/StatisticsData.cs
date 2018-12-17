using System.Collections.Generic;
using Microsoft.ProjectOxford.Face.Contract;

namespace FacialRecognition_Oxford.Data
{
    public class StatisticsData
    {
        public List<FaceAttributes> FaceAttributes { get; set; }
        public int Amount { get; set; }
        public int AmountMale { get; set; }
        public int AmountFemale { get; set; }
        public double Happiness { get; set; }


        public StatisticsData() { FaceAttributes = new List<FaceAttributes>(); }

        /// <summary> Sets the date for this object </summary>
        public void UpdateStatistics(FaceAttributes attribute)
        {
            FaceAttributes.Add(attribute);

            Amount++;

            if (attribute.Gender.ToLower().Equals("male"))
                AmountMale++;
            else
                AmountFemale++;

            Happiness = attribute.Emotion.Happiness;
        }


        /// <summary> Set the happiness data </summary>
        public void UpdateHappiness(double happiness) => Happiness = happiness;

        public override string ToString() =>
            $"Amount: {Amount}, Amount male: {AmountMale}, Amount female: {AmountFemale}";
    }
}