namespace Wingnut.Channels
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class WingnutCommunicationException : Exception
    {
        public WingnutCommunicationException()
        {
        }

        public WingnutCommunicationException(string message) : base(message)
        {
        }

        public WingnutCommunicationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected WingnutCommunicationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}