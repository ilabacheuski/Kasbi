using System;

using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace TSkassa
{
    /// <summary>
    /// 
    /// </summary>
    class Program
    {
        public static string pathDir;
        public static TSSettings TSSettings;
        public static LogFile MainLog;
        public static LogFile Log1C;
        public static KKM Kassa;

        static void Main(string[] args)
        {

            pathDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(pathDir);

            string settingsPath = @"tskassa.cfg";
            try
            {
                TSSettings = JsonConvert.DeserializeObject<TSSettings>(File.ReadAllText(settingsPath));
            }
            catch (Exception e)
            {
                Console.WriteLine(EVENTS.CANNOT_GET_SETTINGS + " " + e.Message);
                return;
            }

            MainLog = new LogFile(TSSettings.MainEncoding, TSSettings.MainLogFileName);
            //Log1C = new LogFile(TSSettings.MainEncoding, TSSettings.LogTo1CFileName, true);
            MainLog.WriteLog(EVENTS.SETTINGS_READED);

            if (args.Length == 0)
            {
                ExitProgram(EVENTS.PARAMETERS_NOT_FOUND);
            }

            if (!GetKKM())
            {
                ExitProgram(EVENTS.WRONG_KKM);
            }
            Kassa.Init();

            string Log1CFileName = pathDir + "\\" + Kassa.SerialNumber + "\\";
            try
            {
                if (!Directory.Exists(Log1CFileName))
                {
                    Directory.CreateDirectory(Log1CFileName);
                }
                Log1C = new LogFile(TSSettings.MainEncoding, Log1CFileName + TSSettings.LogTo1CFileName, true);
            }
            catch (Exception)
            {

                throw;
            }

            if (args[0].ToLower() == "test")
            { 
                Log1C.WriteLog(Kassa.SerialNumber);
                Console.WriteLine(Kassa.SerialNumber);
            } else if (args[0].ToLower()=="check" && args.Length >= 2) {
                string path = @"" + args[1];
                if (!File.Exists(path))
                {
                    ExitProgram(EVENTS.FILE_NOT_FOUND + " " + path);
                }

                Kassa.PrintCheck(path);
            } else if (args[0].ToLower() == "itogi")
            {
                var answer = Kassa.GetSummary();
                Console.WriteLine(BitConverter.ToString(answer));
            }
        }

        public static bool GetKKM()
        {            
            switch (Program.TSSettings.NameOfKKM)
            {
                case "Kasbi02MF":
                    Kassa = new Kasbi02MF();
                    return true;
                default:
                    return false;
            }            
        }

        //public static bool GetSendDataObj()
        //{
        //    switch (Program.TSSettings)
        //    {
        //        default:
        //            break;
        //    }
        //}

        public static void ExitProgram(string Error = null, bool to1CLog = true)
        {
            if (Error != null)
            {
                MainLog.WriteLog(Error);
                if (to1CLog)
                {
                    Log1C?.WriteLog(Error);
                }                
            }
            MainLog = null;
            Environment.Exit(0);
        }
    }
}