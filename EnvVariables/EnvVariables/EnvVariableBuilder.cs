namespace EnvVariables;

public class EnvVariableBuilder
{
    private readonly string _name;
    private string? _description;
    private HashSet<string>? _populateTo;
    private bool _required;
    private string? _defaultValue;
    private Func<string?, string?>? _customValidator;

    public EnvVariableBuilder(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _name = name;
        _required = true;
    }

    public static EnvVariableBuilder Required(string name)
    {
        return new EnvVariableBuilder(name).AsRequired();
    }

    public static EnvVariableBuilder Optional(string name)
    {
        return new EnvVariableBuilder(name).AsOptional();
    }

    public EnvVariableBuilder WithDescription(string description)
    {
        _description = description;

        return this;
    }

    public EnvVariableBuilder WithDefaultValue(string? defaultValue)
    {
        _defaultValue = defaultValue;

        return this;
    }

    public EnvVariableBuilder WithPopulateTo(params string[] populateTo)
    {
        _populateTo = new HashSet<string>(populateTo);

        return this;
    }

    public EnvVariableBuilder WithCustomValidation(Func<string?, string?>? customValidator)
    {
        _customValidator = customValidator;

        return this;
    }

    public EnvVariableBuilder AsRequired()
    {
        _required = true;

        return this;
    }

    public EnvVariableBuilder AsOptional()
    {
        _required = false;

        return this;
    }

    public EnvVariable Build()
    {
        return new EnvVariable(_name, _description, _populateTo, _required, _defaultValue, _customValidator);
    }

    public static implicit operator EnvVariable(EnvVariableBuilder builder)
    {
        return builder.Build();
    }
}
