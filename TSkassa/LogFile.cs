using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TSkassa
{
    class LogFile
    {
        private string logFileName;
        //        private System.IO.FileStream file;
        public int MaxSize { get; private set; }
        public Encoding Encoding { get; set; }
        public bool isSmallLog { get; private set; }

        public LogFile(Encoding Encoding, string fileName = @"ts_kassa.log", bool isSmallLog = false, int MaxLinesInFile = 1000)
        {
            this.logFileName = fileName;
            this.MaxSize = MaxLinesInFile;
            this.Encoding = Encoding;
            this.isSmallLog = isSmallLog;

            if (!System.IO.File.Exists(logFileName))
            {
                try
                {
                    System.IO.File.Create(logFileName).Close();
                }
                catch (SystemException e)
                {
                    Console.WriteLine(e.Message);
                    System.Environment.Exit(-1);
                }                
            }
            if (!isSmallLog)
            {
                WriteLog("================== НАЧАЛО РАБОТЫ ==================");
            }
        }

        ~LogFile()
        {
            if (!isSmallLog)
            {
                WriteLog("================== КОНЕЦ РАБОТЫ ==================");
            }                       
        }

        private void CheckSize(int linesCount = 2)
        {
            var lines = System.IO.File.ReadAllLines(logFileName);
            if (lines.Length > MaxSize)
            {
                try
                {
                    System.IO.File.WriteAllLines(logFileName, lines.Skip(MaxSize - linesCount).ToArray(), Encoding);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }

        public int ClearLogFile()
        {
            int retV = 0;
            try
            {
                System.IO.File.WriteAllText(logFileName, String.Empty, Encoding);
                retV = 1;
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message);
            }
            return retV;
        }

        public void WriteLog(string str)
        {
            CheckSize();
            try
            {
                if (isSmallLog)
                {
                    System.IO.File.WriteAllText(logFileName, str + "\n", Encoding);
                }
                else
                {
                    File.AppendAllText(logFileName, DateTime.Now.ToString() + ":");
                    System.IO.File.AppendAllText(logFileName, str + "\n", Encoding);
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }            
        }

        public void WriteLog(IEnumerable<string> strings)
        {
            CheckSize(strings.Count());
            try
            {
                if (isSmallLog)
                {
                    System.IO.File.WriteAllLines(logFileName, strings, Encoding);
                }
                else
                {
                    File.AppendAllText(logFileName, DateTime.Now.ToString() + ":");
                    System.IO.File.AppendAllLines(logFileName, strings, Encoding);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
