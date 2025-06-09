using System.Diagnostics.CodeAnalysis;

namespace EnvVariables;

public class RequiredVariable : EnvVariable
{
    public RequiredVariable(
        string name,
        string[] populateTo,
        string? defaultValue = null
    ) : base(name, populateTo, required: true, defaultValue)
    {
    }
}

public class OptionalVariable : EnvVariable
{
    public OptionalVariable(
        string name,
        string[] populateTo,
        string? defaultValue = null
    ) : base(name, populateTo, required: false, defaultValue)
    {
    }
}

public class EnvVariable
{
    public string Name { get; init; }

    public string[] PopulateTo { get; init; }

    public bool Required { get; init; }

    public string? DefaultValue { get; init; }

    public EnvVariable(
        string name,
        string[] populateTo,
        bool required,
        string? defaultValue
    )
    {
        Name = name;
        PopulateTo = populateTo;
        Required = required;
        DefaultValue = defaultValue;
    }

    public virtual bool Validate(string? value, [NotNullWhen(returnValue: false)] out string? errorMessage)
    {
        if (!Required || !string.IsNullOrWhiteSpace(value))
        {
            errorMessage = null;

            return true;
        }

        errorMessage = "Environment variable {Name} is missing";

        return false;
    }
}
