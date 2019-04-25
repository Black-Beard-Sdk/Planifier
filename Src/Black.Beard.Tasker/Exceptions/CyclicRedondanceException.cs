using System;
using System.Collections.Generic;
using System.Text;

namespace Black.Beard.Tasker.Exceptions
{


    [Serializable]
    public class CyclicRedondanceException : Exception
    {
        public CyclicRedondanceException() { }
        public CyclicRedondanceException(string message) : base(message) { }
        public CyclicRedondanceException(string message, Exception inner) : base(message, inner) { }
        protected CyclicRedondanceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
