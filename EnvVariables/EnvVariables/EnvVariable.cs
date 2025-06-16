using System.Diagnostics.CodeAnalysis;

namespace EnvVariables;

public delegate bool EnvVariableValidation(string? value, [NotNullWhen(returnValue: false)] out string? errorMessage);

public class EnvVariable : IEquatable<EnvVariable>
{
    private EnvVariableValidation _customValidation;
    private readonly HashSet<string> _populateTo;

    public string Name { get; }

    public IReadOnlyCollection<string> PopulateTo => _populateTo;

    public bool Required { get; private set; }

    public string? DefaultValue { get; private set; }

    public EnvVariable(
        string name,
        IReadOnlyCollection<string>? populateTo = null,
        bool required = true,
        string? defaultValue = null,
        EnvVariableValidation? customValidation = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _populateTo = new HashSet<string>(populateTo ?? []);
        Name = name;
        Required = required;
        DefaultValue = defaultValue;
        _customValidation = customValidation ?? DefaultValidation;
    }

    public static EnvVariable AsRequired(string name, string[]? populateTo = null, string? defaultValue = null)
    {
        return new EnvVariable(name, populateTo, required: true, defaultValue);
    }

    public static EnvVariable AsOptional(string name, string[]? populateTo = null, string? defaultValue = null)
    {
        return new EnvVariable(name, populateTo, required: false, defaultValue);
    }

    public void MergeWith(EnvVariable envVariable)
    {
        if (this != envVariable)
        {
            throw new InvalidOperationException("Cannot merge envs with different names");
        }

        if (_customValidation != DefaultValidation && envVariable._customValidation != DefaultValidation)
        {
            throw new InvalidOperationException("Cannot merge envs with different custom validations");
        }

        Required |= envVariable.Required;

        if (!string.IsNullOrWhiteSpace(envVariable.DefaultValue) && string.IsNullOrWhiteSpace(envVariable.DefaultValue))
        {
            DefaultValue = envVariable.DefaultValue;
        }

        _populateTo.UnionWith(envVariable.PopulateTo);

        // default validation might be replaced with custom
        if (_customValidation == DefaultValidation && envVariable._customValidation != envVariable.DefaultValidation)
        {
            _customValidation = envVariable._customValidation;
        }
    }

    public bool Validate(string? value, [NotNullWhen(returnValue: false)] out string? errorMessage)
    {
        return _customValidation(value, out errorMessage);
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

    private bool DefaultValidation(string? value, [NotNullWhen(returnValue: false)] out string? errorMessage)
    {
        if (Required && string.IsNullOrWhiteSpace(value))
        {

            errorMessage = $"Environment variable {Name} is missing";

            return false;
        }

        errorMessage = null;

        return true;
    }
}
