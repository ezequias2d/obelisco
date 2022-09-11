using System;
using System.Runtime.Serialization;

namespace Obelisco
{
    public class ResponseErrorException : Exception
    {
        public ResponseErrorException()
        {
        }

        public ResponseErrorException(string message) : base(message)
        {
        }

        public ResponseErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ResponseErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}