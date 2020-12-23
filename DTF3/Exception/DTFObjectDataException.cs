namespace DTF3.Exception
{
    public class DTFObjectDataException : System.Exception
    {
        public DTFObjectDataException(string message)
        {
            Message = message;
        }

        public override string Message { get; }
    }
}