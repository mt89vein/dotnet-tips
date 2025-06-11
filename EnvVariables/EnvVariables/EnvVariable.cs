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

public class EnvVariable : IEquatable<EnvVariable>
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

    public bool Equals(EnvVariable? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((EnvVariable)obj);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
    }

    public static bool operator ==(EnvVariable? left, EnvVariable? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EnvVariable? left, EnvVariable? right)
    {
        return !Equals(left, right);
    }
}
