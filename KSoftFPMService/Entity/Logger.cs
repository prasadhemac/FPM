using System;
using System.Collections.Generic;
using System.Text;

namespace KSoftFingerPrintManager.Entity
{
    static class Logger
    {
        private static String fileName = "log";

        
        public static void log(String Logline)
        {
            Logline += "\n";
            System.IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + fileName, Logline);
        }
    }
}
