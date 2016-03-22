using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rex.Utilities.Helpers
{
    [Serializable]
    public class AccessingDeletedVariableException : Exception
    {
        public string VarName { get; set; }

        public AccessingDeletedVariableException() { }
        public AccessingDeletedVariableException(string message) : base(message) { }
        public AccessingDeletedVariableException(string message, Exception inner) : base(message, inner) { }
        protected AccessingDeletedVariableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }
}
