namespace EnvVariables;

public sealed class InvalidEnvVariablesException : Exception
{
    public IReadOnlyDictionary<string, string> Errors { get; }

    public InvalidEnvVariablesException(string message, IReadOnlyDictionary<string, string> errors)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(errors);

        Errors = errors;
    }

    public override string ToString()
    {
        return Message + " " + string.Join(", ", Errors.Select(x => $"{x.Key}: {x.Value}"));
    }
}
