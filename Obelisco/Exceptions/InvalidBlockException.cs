using System;
using System.Runtime.Serialization;

namespace Obelisco
{
    public class InvalidBlockException : Exception
    {
        public InvalidBlockException()
        {
        }

        public InvalidBlockException(string message) : base(message)
        {
        }

        public InvalidBlockException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidBlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}