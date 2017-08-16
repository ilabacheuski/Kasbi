using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace TSkassa
{
    abstract class KKM
    {
        public abstract string SerialNumber { get; set; }
        public abstract string Name { get; set; }

        public abstract void Init(int NumberOfKKM = 1);

        public abstract void PrintCheck(string path);
        public abstract void ClearBuffer();
        public abstract void ClearBuffer(object Buffer);
        public abstract void ClearBuffer(object[] Buffers);
        public abstract void FillBufferEx(object BufferData);
        public abstract byte[] GetSummary();
    }
}
