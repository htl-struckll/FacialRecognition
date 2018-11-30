using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face.Contract;

namespace FacialRecognition_Oxford.Misc
{
    class Helper
    {

        /// <summary>
        /// Get the dominant emotion as string
        /// </summary>
        /// <param name="scores"></param>
        /// <returns></returns>
        public static string GetDominantEmotionAsString(EmotionScores scores) => scores.ToRankedList().First().Key;

        /// <summary>
        /// Get the face attributes as string
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static string GetFaceAttributesAsString(FaceAttributes attributes)
        {
            List<string> retVal = new List<string>();

            if(attributes.Gender != null)
                retVal.Add(attributes.Gender);
            if(attributes.Age > 0)
                retVal.Add("Age: " + attributes.Age);

            return string.Join(", ", retVal);
        }

        /// <summary>
        /// Logging function
        /// </summary>
        /// <param name="msg"></param>
        public static void ConsoleLog(string msg) =>
            Console.WriteLine("[" + DateTime.Now.ToShortTimeString() + "] " + msg);

    }
}
