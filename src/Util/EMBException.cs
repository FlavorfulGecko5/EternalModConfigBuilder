class EMBException : Exception
{
    public EMBException(string message) : base(message)
    {
    }

    public static EMBException buildException(string msg, string[] args)
    {
        string formattedMsg = String.Format(msg, args);
        return new EMBException(formattedMsg);
    }
}