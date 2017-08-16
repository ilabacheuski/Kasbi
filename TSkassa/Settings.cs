using System;
using System.IO;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TSkassa
{
    
    class TSSettings
    {
        //Настройки порта
        public string PortN { get; set; }
        public int BaudRate { get; set; }
        public System.IO.Ports.Parity Parity { get; set; }
        public int DataBits { get; set; }
        public System.IO.Ports.StopBits StopBits { get; set; }
        public int MaxSleepTime { get; set; }
        //Логи
        public string MainLogFileName { get; set; }
        public string LogTo1CFileName { get; set; }
        public Encoding MainEncoding { get; set; }
        //Файл настроек || пока не используется
        //public string SettingsFileName { get; set; }
        //Настройки кассы
        public string NameOfKKM { get; set; }
        public bool TestMode { get; set; }
        public string QueriesFile;


        [JsonConstructor]
        public TSSettings(string portN, int baudRate, string parity, int dataBits, string stopBits, int maxSleepTime,
                            string mainLogFileName, string logTo1CFileName, string mainEncoding, string nameOfKKM, string queriesFile)
        {
            PortN = portN; BaudRate = baudRate; DataBits = dataBits; MaxSleepTime = maxSleepTime;
            MainLogFileName = mainLogFileName; LogTo1CFileName = logTo1CFileName; NameOfKKM = nameOfKKM;
            switch (parity.Trim())
            {
                case "Even":
                    Parity = System.IO.Ports.Parity.Even;
                    break;
                case "Mark":
                    Parity = System.IO.Ports.Parity.Mark;
                    break;
                case "Odd":
                    Parity = System.IO.Ports.Parity.Odd;
                    break;
                case "Space":
                    Parity = System.IO.Ports.Parity.Space;
                    break;
                case "None":
                default:
                    Parity = System.IO.Ports.Parity.None;
                    break;
            }
            switch (stopBits.Trim())
            {
                case "None":
                    StopBits = System.IO.Ports.StopBits.None;
                    break;
                case "OnePointFive":
                    StopBits = System.IO.Ports.StopBits.OnePointFive;
                    break;
                case "Two":
                    StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case "One":
                default:
                    StopBits = System.IO.Ports.StopBits.One;
                    break;
            }
            try
            {
                MainEncoding = Encoding.GetEncoding(mainEncoding);
            }
            catch (Exception)
            {
                MainEncoding = Encoding.ASCII;
            }

            if (String.IsNullOrEmpty(queriesFile))
            {
                QueriesFile = @"Kasbi02MF.queries";
                if (!File.Exists(QueriesFile))
                {
                    Program.ExitProgram(EVENTS.NO_QUERIES_TEMPLATE + " " + QueriesFile);
                }
            }
            else
            {
                QueriesFile = queriesFile;
            }
        }
    }
}
