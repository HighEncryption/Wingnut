namespace Wingnut.Data.Models
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class NutCommunicationException : Exception
    {
        public NutCommunicationException()
        {
        }

        public NutCommunicationException(string message)
            : base(message)
        {
        }

        public NutCommunicationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NutCommunicationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}