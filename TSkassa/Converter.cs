using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace TSkassa
{
    class Converter
    {
        public ulong ToULong(byte[] byteArray)
        {
            ulong retV = 0;
            string buffer = "";
            buffer = BitConverter.ToString(byteArray).Replace("-", String.Empty);
            int i = 0;
            for (i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != '0') break;
            }
            buffer = "0x" + buffer.Substring(i);
            try
            {
                retV = Convert.ToUInt64(buffer, 16);
            }
            catch (Exception)
            {

                retV = 0;
            }
            return retV;
        }
    }
}
