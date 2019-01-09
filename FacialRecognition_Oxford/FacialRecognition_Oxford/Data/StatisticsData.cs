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
        public List<HairColorType> HairColors { get; set; }


        public StatisticsData()
        {
            FaceAttributes = new List<FaceAttributes>();
            HairColors = new List<HairColorType>();
        }

        /// <summary> Sets the date for this object </summary>
        public void UpdateStatistics(FaceAttributes attribute)
        {
            FaceAttributes.Add(attribute);

            Amount++;

            if (attribute.Gender.ToLower().Equals("male"))
                AmountMale++;
            else
                AmountFemale++;

            HairColors.Add( GetDominantHairColor(attribute.Hair.HairColor));
            Happiness = attribute.Emotion.Happiness;
        }

        /// <summary>
        /// Gets the dominant color
        /// </summary>
        /// <param name="hairColour">Haircolor array</param>
        /// <returns>The dominant haircolour</returns>
        private HairColorType GetDominantHairColor(HairColor[]  hairColour)
        {
            HairColorType retVal = HairColorType.Black;
            double bestConfidence = 0;

            for (int idx = 0; idx < hairColour.Length; idx++)
            {
                if(bestConfidence < hairColour[idx].Confidence)
                {
                    bestConfidence = hairColour[idx].Confidence;
                    retVal = hairColour[idx].Color;
                }
            }

            return retVal;
        }

        /// <summary> Set the happiness data </summary>
        public void UpdateHappiness(double happiness) => Happiness = happiness;

        public override string ToString() =>
            $"Amount: {Amount}, Amount male: {AmountMale}, Amount female: {AmountFemale}";
    }
}