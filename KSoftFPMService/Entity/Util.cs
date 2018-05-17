using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KSoftFingerPrintManager.Business;

namespace KSoftFingerPrintManager.Entity
{
    static class Util
    {
        private static string fileName = "last_time.txt";

        public static void UpdateTime(String time)
        {
            if (time.Equals(String.Empty))
            {
                return;
            }
            // if do not exists it creates it.
            FileInfo FileInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + fileName);
            if (true == FileInfo.Exists)
            {
                // remove the hidden attribute from the file
                FileInfo.Attributes &= ~FileAttributes.Hidden;
            } //if it doesn't exist StreamWriter will create it
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + fileName, time);
            // set the file as hidden
            FileInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + fileName);
            FileInfo.Attributes |= FileAttributes.Hidden;
        }
        
        public static String GetLastEntryTime()
        {
            String line = String.Empty;
            // if do not exists it creates it.
            FileInfo FileInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + fileName);
            if (true == FileInfo.Exists)
            {
                // remove the hidden attribute from the file
                FileInfo.Attributes &= ~FileAttributes.Hidden;
            } //if it doesn't exist StreamWriter will create it
            try
            {
                line = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + fileName);
                // set the file as hidden
                FileInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + fileName);
                FileInfo.Attributes |= FileAttributes.Hidden;
                return line;

            }
            catch (Exception ex)
            {
                Logger.log("EXCEPTION - GetLastEntryTime - " + ex.Message);
                return line;
            }
            
           
        }
        public static DateTime ConvertToDateTime(String dateTime)
        {
            DateTime outputDateTimeValue = MinDateTime();

            if(dateTime.Equals(String.Empty))
            {
                return outputDateTimeValue;
            }
            try
            {
                outputDateTimeValue = DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch(Exception ex)
            {
                Logger.log("EXCEPTION - ConvertToDataTime - " + ex.Message);
            }
            return outputDateTimeValue;
            
        }
        public static DateTime MinDateTime()
        {
            String dateTime = "2010-01-01 00:00:00";
            DateTime output;
            
            output = DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            return output;
        }
        public static DateTime GetLastEntry()
        {
            return ConvertToDateTime(GetLastEntryTime());
        }
    }
}
