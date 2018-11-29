using System;
using System.Collections.Generic;
using System.Linq;
using FacialRecognition_Azure.Data.Enumteration;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace FacialRecognition_Azure.Data
{
    public class Person
    {
        public DetectedFace DetectedFace { get; set; }
        public Gender? Gender { get; set; }
        public double? Age { get; set; }
        public HairColorType Haircolor { get; set; }
        public HairType Hairtype { get; set; }
        public string Emotion { get; set; }
        public GlassesType? GlassesType { get; set; }

        public Person(DetectedFace detectedFace)
        {
            DetectedFace = detectedFace;

            GenerateAttributeData(DetectedFace.FaceAttributes);
        }

        /// <summary>
        /// Generates the data for the attributes
        /// </summary>
        private void GenerateAttributeData(FaceAttributes face)
        {
            Gender = face.Gender;
            Age = face.Age;
            GlassesType = face.Glasses;

            Hairtype = GetHairType(face.Hair);
            Haircolor = GetHairColor(face.Hair.HairColor);
            Emotion = GetFacialExpression(face.Emotion);
        }


        #region Gets

        /// <summary>
        /// Gets the facial expression with the highest value
        /// </summary> 
        /// <param name="em"></param>
        /// <returns></returns>
        private string GetFacialExpression(Emotion em) => (em.GetType().GetProperties().First(info =>
            em.GetType().GetProperties().Max(propertyInfo => (double) propertyInfo.GetValue(em, null)) >=
            (double) info.GetValue(em, null)).Name);

        /// <summary>
        /// Gets if he has bald hair or not
        /// </summary>
        /// <param name="hair"></param>
        /// <returns></returns>
        private HairType GetHairType(Hair hair) =>
            hair.Bald > 90 ? HairType.Bald : HairType.NotBald;

        /// <summary>
        /// Gets the hair colour //todo check with hairColors.max
        /// </summary>
        /// <param name="hairColors">List of haircolours found</param>
        /// <returns>The hair colour of the person</returns>
        private HairColorType GetHairColor(IList<HairColor> hairColors)
        {
            double confidence = 0;
            HairColorType retVal = new HairColorType();

            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence > confidence)
                {
                    confidence = hairColor.Confidence;
                    retVal = hairColor.Color;
                }
            }

            return retVal;
        }

        #endregion

        public override string ToString() =>
            $"{Gender.ToString()}, Hair: {Hairtype}, Haircolor: {Haircolor.ToString()}, Emotion: {Emotion}, Glasses: {GlassesType}";
    }
}
