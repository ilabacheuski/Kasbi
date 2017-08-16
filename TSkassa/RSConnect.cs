using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace TSkassa
{
    class RSConnect
    {
        private string PortN;
        private int BaudRate;        
        private Parity Parity;
        private int DataBits;
        private StopBits StopBits;
        private SerialPort Rs_port;
        protected int MaxSleepTime;
        protected int SleepIteration;
        //private bool _continue;

        public RSConnect()
        {
            PortN      = Program.TSSettings.PortN;
            BaudRate   = Program.TSSettings.BaudRate;
            Parity     = Program.TSSettings.Parity;
            DataBits   = Program.TSSettings.DataBits;
            StopBits   = Program.TSSettings.StopBits;
            MaxSleepTime = Program.TSSettings.MaxSleepTime;
            SleepIteration = 90;

            try
            {
                Rs_port = new SerialPort(PortN, BaudRate, Parity, DataBits, StopBits);
                if (Rs_port.IsOpen)
                {
                    Rs_port.Close();
                }
            }
            catch (Exception e)
            {
                Program.ExitProgram(EVENTS.CANNOT_OPEN_PORT + " " + e.Message);
            }
            Rs_port.ReadTimeout = 6000;
        }

        public int SendToPort(byte[] data, ref byte[] answer, int AddWait = 10)
        {
            //int retV = -1;

            if (Program.TSSettings.TestMode)
            {
                answer = new byte[5] { data[0], data[1], data[2], (byte)0, (byte)0};
                answer[4] = CalcCRC(answer);
                return 0;
            }
            if (Rs_port == null)
            {
                Program.ExitProgram(EVENTS.RS_OBJ_IS_NULL);
            }
            if (!Rs_port.IsOpen)
            {
                try
                {
                    Rs_port.Open();
                }
                catch (Exception e)
                {
                    Program.ExitProgram(EVENTS.CANNOT_OPEN_PORT + " " + e.Message);
                }
            }

            try
            {
                Rs_port.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Program.ExitProgram(EVENTS.CANNOT_WRITE_PORT + " " + e.Message);                
            }

            //_continue = true;
            //Thread readThread = new Thread(Read);
            //readThread.Start();
            //readThread.Join();

            //byte[] buffer = new byte[512];
            //Action kickoffRead = null;
            //kickoffRead = delegate {
            //    Rs_port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar) {
            //        try
            //        {
            //            int actualLength = Rs_port.BaseStream.EndRead(ar);
            //            byte[] received = new byte[actualLength];
            //            Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
            //            //raiseAppSerialDataEvent(received);
            //            retV = 1;
            //        }
            //        catch (System.IO.IOException exc)
            //        {
            //            Program.MainLog.WriteLog(TSkassa.EVENTS.NO_ANSWER_KKM + " " + exc.Message);
            //            //handleAppSerialError(exc);
            //        }
            //        kickoffRead();
            //    }, null);
            //};
            //kickoffRead();

            //int readedBytes = (int)buffer[1] + 4;
            //answer = new byte[readedBytes];
            //Buffer.BlockCopy(buffer, 0, answer, 0, readedBytes);
            //retV = 1;
            //return retV;

            //bool tt = false;

            try
            {
             //   int curSleepTime = 0;
                Thread.Sleep(SleepIteration+ AddWait);
                byte[] buffer = new byte[3];
                for (int i = 0; i < 3; i++)
                {
                    buffer[i] = (byte)Rs_port.ReadByte();
                }

                answer = new byte[(int)buffer[1] + 4];

                answer[0] = buffer[0];
                answer[1] = buffer[1];
                answer[2] = buffer[2];

                for (int i = 0; i < buffer[1]; i++)
                {
                    answer[i + 3] = (byte)Rs_port.ReadByte();
                }

                answer[answer.Length - 1] = CalcCRC(answer);

                int crcFromKKM = Rs_port.ReadByte();

                if (crcFromKKM != answer[answer.Length - 1])
                {
                    Program.MainLog.WriteLog("ERROR: Не совпадают CRC от ККМ и CRC вычисленное в ответе порта!");
                }

                Rs_port.Close();
            }
            catch (Exception e)
            {
                Program.MainLog.WriteLog(e.Message);
            }
            

            return 1;

            //while (curSleepTime < MaxSleepTime)
            //{
            //    curSleepTime = curSleepTime + SleepIteration;
            //    System.Threading.Thread.Sleep(SleepIteration);
            //    if (Rs_port.BytesToRead > 0) { break; }
            //}

            //if (Rs_port.BytesToRead > 0)
            //{
            //    int BytesToRead = Rs_port.BytesToRead;
            //    if (data[2] == (byte)25)
            //    {
            //        System.Threading.Thread.Sleep(150);
            //        BytesToRead = 160;
            //    }
            //    if (data[2] == (byte)18) // закрытие чека.
            //    {
            //        BytesToRead = 31;
            //    }
            //    try
            //    {
            //        answer = new byte[BytesToRead];
            //        Rs_port.Read(answer, 0, BytesToRead);
            //        Rs_port.Close();
            //        return 1;
            //    }
            //    catch (Exception e)
            //    {
            //        Program.MainLog.WriteLog(e.Message);
            //        return 0;
            //    }

            //}
            //else
            //{
            //    Rs_port.Close();
            //    Program.MainLog.WriteLog(TSkassa.EVENTS.NO_ANSWER_KKM);
            //    return -1;
            //}
        }

        //private void Read()
        //{
        //    while (_continue)
        //    {
        //        try
        //        {
        //            string message = Rs_port.ReadLine();
        //            Console.WriteLine(message);
        //        }
        //        catch (TimeoutException e)
        //        {
        //            Program.Log1C.WriteLog("ERROR:" + e.Message);
        //        }
        //    }
        //}

        public byte CalcCRC(byte[] mas)
        {
            byte crc = mas[0];

            for (int i = 1; i < mas.Length; i++)
                crc = (byte)(crc + mas[i]);

            return (byte)(crc ^ 0xFF);
        }
    }
}
