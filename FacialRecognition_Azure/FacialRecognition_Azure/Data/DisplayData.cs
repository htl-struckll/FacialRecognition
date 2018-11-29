using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Person = FacialRecognition_Azure.Data.Person;
//todo
namespace FacialDetection_Emgu.Data
{
    public class DisplayData
    {
        public IList<Person> Persons { get; set; }
        public double? MalePercentage { get; set; }
        public double? FemalePercentage { get; set; }
        public double? AverageAge { get; set; }
        public GlassesType MostUsedGlassType { get; set; }

        /// <summary>
        /// Generates the data
        /// </summary>
        public void GenerateData()
        {
            int cntMale = 0, cntFemale = 0, cntPersons = Persons.Count, cntNoGlasses = 0, cntReadingGlasses = 0, cntSunGlasses = 0;
            double? age = 0;
            foreach (Person person in Persons)
            {
                if (person.Gender.Equals(Gender.Male))
                    cntMale++;
                else
                    cntFemale++;

                age += person.Age;

                if (person.GlassesType.Equals(GlassesType.NoGlasses))
                    cntNoGlasses++;
                else if (person.GlassesType.Equals(GlassesType.ReadingGlasses))
                    cntReadingGlasses++;
                else if (person.GlassesType.Equals(GlassesType.Sunglasses))
                    cntSunGlasses++;
            }

            MalePercentage = (cntPersons / 100) * cntMale;
            FemalePercentage = (cntPersons / 100) * cntFemale;
            AverageAge = age / cntPersons;

            if (cntNoGlasses > cntReadingGlasses && cntNoGlasses > cntSunGlasses)
                MostUsedGlassType = GlassesType.NoGlasses;
            else if (cntReadingGlasses > cntNoGlasses && cntReadingGlasses > cntSunGlasses)
                MostUsedGlassType = GlassesType.Sunglasses;
            else if (cntSunGlasses > cntReadingGlasses && cntSunGlasses > cntNoGlasses)
                MostUsedGlassType = GlassesType.Sunglasses;
        }
    }
}
