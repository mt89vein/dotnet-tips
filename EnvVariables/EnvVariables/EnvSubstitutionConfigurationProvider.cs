namespace EnvVariables;

public sealed class EnvSubstitutionConfigurationProvider : ConfigurationProvider
{
    private readonly ILogger _logger;
    private readonly IReadOnlyCollection<EnvVariable> _envVariables;

    public EnvSubstitutionConfigurationProvider(ILogger logger, IReadOnlyCollection<EnvVariable> envVariables)
    {
        _logger = logger;
        _envVariables = envVariables;
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
}

public sealed class EnvSubstitutionConfigurationSource : IConfigurationSource
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IReadOnlyCollection<EnvVariable> _mapping;

    public EnvSubstitutionConfigurationSource(
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> mapping
    )
    {
        _loggerFactory = loggerFactory;
        _mapping = mapping;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EnvSubstitutionConfigurationProvider(_loggerFactory.CreateLogger<EnvSubstitutionConfigurationProvider>(), _mapping);
    }

    public void PrintEnvs()
    {
        var logger = _loggerFactory.CreateLogger<EnvSubstitutionConfigurationSource>();

        logger.LogInformation("Printing configured env variables:");

        foreach (var envVariable in _mapping)
        {
            logger.LogInformation("{@EnvVariable}", envVariable);
        }
    }
}
