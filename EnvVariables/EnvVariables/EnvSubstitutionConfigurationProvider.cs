namespace EnvVariables;

public sealed class EnvSubstitutionConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    private readonly ILogger _logger;
    private readonly HashSet<EnvVariable> _envVariables;

    public EnvSubstitutionConfigurationProvider(IReadOnlyCollection<EnvVariable> envVariables, ILogger logger)
    {
        _logger = logger;
        _envVariables = new HashSet<EnvVariable>(envVariables);
    }

    public void Add(params EnvVariable[] envVariables)
    {
        foreach (var envVariable in envVariables)
        {
            _envVariables.Add(envVariable);
        }
        Load();
        OnReload();
    }

    public override void Load()
    {
        var hasErrors = false;
        foreach (var envVariable in _envVariables)
        {
            var value = Environment.GetEnvironmentVariable(envVariable.Name) ?? envVariable.DefaultValue;

            if (!envVariable.Validate(value, out var errorMessage))
            {
                _logger.LogCritical(errorMessage, envVariable.Name);
                hasErrors = true;
            }

            foreach (var section in envVariable.PopulateTo)
            {
                Data[section] = value;
            }
        }

        if (hasErrors)
        {
            Environment.Exit(1);
        }
    }

    public void PrintEnvs()
    {
        _logger.LogInformation("Printing configured env variables:");

        foreach (var envVariable in _envVariables)
        {
            _logger.LogInformation("{@EnvVariable}", envVariable);
        }
    }

    public IReadOnlyCollection<EnvVariable> GetEnvs()
    {
        return _envVariables;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return this;
    }
}
