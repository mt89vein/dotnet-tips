namespace EnvVariables;

public sealed class EnvSubstitutionConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    private readonly ILogger _logger;
    private readonly HashSet<EnvVariable> _envVariables;
    private readonly Func<IEnumerable<EnvVariable>>? _dynamicVariables;

    public EnvSubstitutionConfigurationProvider(
        IReadOnlyCollection<EnvVariable> envVariables,
        ILogger logger,
        Func<IEnumerable<EnvVariable>>? dynamicVariables = null
    )
    {
        _dynamicVariables = dynamicVariables;
        _logger = logger;
        _envVariables = new HashSet<EnvVariable>(envVariables.Count);

        AddVariables(envVariables);
    }

    public void Add(params EnvVariable[] envVariables)
    {
        AddVariables(envVariables);
        Load();
        OnReload();
    }

    public override void Load()
    {
        var errors = new List<string>();

        foreach (var envVariable in GetAllVariables())
        {
            var value = Environment.GetEnvironmentVariable(envVariable.Name) ?? envVariable.DefaultValue;

            if (!envVariable.Validate(value, out var errorMessage))
            {
                _logger.LogCritical(errorMessage);
                errors.Add(errorMessage);
            }

            foreach (var section in envVariable.PopulateTo)
            {
                Data[section] = value;
            }
        }

        if (errors.Count != 0)
        {
            throw new InvalidOperationException($"Environment variable validation failed: {string.Join("; ", errors)}");
        }
    }

    public IEnumerable<EnvVariable> GetAllVariables()
    {
        return _envVariables.Concat(_dynamicVariables?.Invoke() ?? []);
    }

    public void PrintEnvs()
    {
        _logger.LogInformation("{@EnvVariables}", GetAllVariables());
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return this;
    }

    private void AddVariables(IReadOnlyCollection<EnvVariable> envVariables)
    {
        foreach (var envVariable in envVariables)
        {
            if (!_envVariables.Add(envVariable))
            {
                var env = _envVariables.Single(x => x == envVariable);
                env.MergeWith(envVariable);
            }
        }
    }
}
