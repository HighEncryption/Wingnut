namespace Wingnut.Data
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class WingnutException : Exception
    {
        public WingnutException(string message) : base(message)
        {
        }

        public WingnutException(string message, Exception inner) : base(message, inner)
        {
        }


        protected WingnutException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}