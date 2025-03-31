namespace SomeService;

public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {

    }
}
