using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace server_console
{
    class ColorConsoleOutput
    {
        public static void YellowEvent(params string[] pMessage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            string messageString = string.Concat(pMessage);
            Console.WriteLine(messageString);
            Console.ResetColor();
        }
    }
}
