namespace DTF3.Exception
{
    public class DTFException : System.Exception
    {
        public DTFException(string message)
        {
            Message = message;
        }

        public override string Message { get; }
    }
}