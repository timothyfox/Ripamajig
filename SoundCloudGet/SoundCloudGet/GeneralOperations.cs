using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoundCloudGet
{
    public static class GeneralOperations
    {
        static ConsoleColor _defaultColour = Console.ForegroundColor;
        public static int _pageSize = 25;

        public static void Log(string s, ConsoleColor col, bool returnColour = true)
        {
            var orig = Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.WriteLine(s);
            if (returnColour)
                Console.ForegroundColor = orig;
        }

        public static void LogLines(string[] result)
        {
            foreach (string r in result)
            {
                Log(r, ConsoleColor.Yellow);
            }
        }

        public static void LogWithoutLine(string s, ConsoleColor col, bool returnColour = true)
        {
            var orig = Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.Write(s);
            if (returnColour)
                Console.ForegroundColor = orig;
        }

        public static void RestoreColour()
        {
            Console.ForegroundColor = _defaultColour;
        }
    }
}
