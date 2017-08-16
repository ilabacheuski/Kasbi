using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSkassa
{

    [Serializable]
    public class PaymentException : Exception
    {
        public string msg;
        public PaymentException() { }
        public PaymentException(string message) : base(message) { }
        public PaymentException(string message, Exception inner) : base(message, inner) { }
        protected PaymentException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }
}
