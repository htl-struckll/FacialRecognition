using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        /// <returns>The dominant emotion as a string</returns>
        public static string GetDominantEmotionAsString(EmotionScores scores) => scores.ToRankedList().First().Key;

        /// <summary>
        /// Get the face attributes as string
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns>The face attributes as a string</returns>
        public static string GetFaceAttributesAsString(FaceAttributes attributes)
        {
            List<string> retVal = new List<string>();

            if(attributes.Gender != null)
                retVal.Add("Gender: " + attributes.Gender);
            

            return string.Join(", ", retVal);
        }

        /// <summary>
        /// Logging function
        /// </summary>
        /// <param name="msg"></param>
        public static void ConsoleLog(string msg) =>
            Console.WriteLine(@"[" + DateTime.Now.ToLongTimeString() + @"] " + msg);

        public static void SetColour() => Console.BackgroundColor = ConsoleColor.Blue;

        /// <summary>
        /// Window display function
        /// </summary>
        /// <param name="msg">Message</param>
        /// <param name="caption">Caption</param>
        /// <param name="btn">Button</param>
        /// <param name="icon">icon</param>
        public static void WindowLog(string msg, string caption = "Info", MessageBoxButton btn = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.Information) => MessageBox.Show(msg, caption, btn, icon);

    }
}
