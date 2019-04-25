using System;
using System.Collections.Generic;
using System.Text;

namespace Bb.Exceptions
{


    [Serializable]
    public class TaskNodeException : Exception
    {
        public TaskNodeException() { }
        public TaskNodeException(string message) : base(message) { }
        public TaskNodeException(string message, Exception inner) : base(message, inner) { }
        protected TaskNodeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
