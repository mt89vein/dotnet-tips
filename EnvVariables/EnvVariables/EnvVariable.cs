using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace EnvVariables;

/// <summary>
/// Environment variable.
/// </summary>
[DebuggerDisplay("DebuggerDisplayString()")]
public class EnvVariable
{
    internal static IEqualityComparer<EnvVariable> NameComparer { get; } = new NameEqualityComparer();

    private Func<string?, string?> _customValidation;
    private readonly HashSet<string> _populateTo;

    public string Name { get; }

    public string? Description { get; private set; }

    public IReadOnlyCollection<string> PopulateTo => _populateTo;

    public bool Required { get; private set; }

    public string? DefaultValue { get; private set; }

    public EnvVariable(
        string name,
        string? description = null,
        IReadOnlyCollection<string>? populateTo = null,
        bool required = true,
        string? defaultValue = null,
        Func<string?, string?>? customValidation = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _populateTo = new HashSet<string>(populateTo ?? []);
        Name = name;
        Description = description;
        Required = required;
        DefaultValue = defaultValue;
        _customValidation = customValidation ?? DefaultValidation;
    }

    public override string ToString()
    {
        return Name;
    }

    internal void MergeWith(EnvVariable envVariable)
    {
        if (!NameComparer.Equals(this, envVariable))
        {
            throw new InvalidOperationException($"Cannot merge envs with different names [{envVariable.Name} -> {Name}]");
        }

        if (_customValidation != DefaultValidation && envVariable._customValidation != DefaultValidation)
        {
            throw new InvalidOperationException("Cannot merge envs with different custom validations");
        }

        Required |= envVariable.Required;

        if (!string.IsNullOrWhiteSpace(envVariable.DefaultValue) && string.IsNullOrWhiteSpace(DefaultValue))
        {
            DefaultValue = envVariable.DefaultValue;
        }

        if (!string.IsNullOrWhiteSpace(envVariable.Description) && string.IsNullOrWhiteSpace(Description))
        {
            Description = envVariable.Description;
        }

        _populateTo.UnionWith(envVariable.PopulateTo);

        // default validation might be replaced with custom
        if (_customValidation == DefaultValidation && envVariable._customValidation != envVariable.DefaultValidation)
        {
            _customValidation = envVariable._customValidation;
        }
    }

    internal bool Validate(string? value, [NotNullWhen(returnValue: false)] out string? errorMessage)
    {
        errorMessage = _customValidation(value);

        return string.IsNullOrWhiteSpace(errorMessage);
    }

    private string? DefaultValidation(string? value)
    {
        if (Required && string.IsNullOrWhiteSpace(value))
        {
            return $"Environment variable {Name} is missing";
        }

        return null;
    }

    private string DebuggerDisplayString()
    {
        return Name +
               (Required
                   ? "*"
                   : "") +
               " = " +
               (Environment.GetEnvironmentVariable(Name) ?? DefaultValue);
    }

    private sealed class NameEqualityComparer : IEqualityComparer<EnvVariable>
    {
        public bool Equals(EnvVariable? x, EnvVariable? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(EnvVariable obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
        }
    }
}
