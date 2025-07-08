namespace EnvVariables;

internal sealed class EnvSubstitutionConfigurationProvider : ConfigurationProvider, IConfigurationSource, IEnvVariablesProvider
{
    private readonly HashSet<EnvVariable> _envVariables;
    private readonly Func<IEnumerable<EnvVariable>>? _dynamicVariables;

    public EnvSubstitutionConfigurationProvider(
        IReadOnlyCollection<EnvVariable> envVariables,
        Func<IEnumerable<EnvVariable>>? dynamicVariables = null
    )
    {
        _dynamicVariables = dynamicVariables;
        _envVariables = new HashSet<EnvVariable>(envVariables.Count, EnvVariable.NameComparer);

        AddVariables(envVariables);
    }

    public void Add(IReadOnlyCollection<EnvVariable> envVariables)
    {
        ArgumentNullException.ThrowIfNull(envVariables);

        AddVariables(envVariables);
        Load();
        OnReload();
    }

    public override void Load()
    {
        var errors = new Dictionary<string, string>();

        foreach (var envVariable in GetAll())
        {
            var value = Environment.GetEnvironmentVariable(envVariable.Name) ?? envVariable.DefaultValue;

            try
            {
                if (!envVariable.Validate(value, out var errorMessage))
                {
                    errors.TryAdd(envVariable.Name, errorMessage);
                }
            }
            catch (Exception e)
            {
                errors.TryAdd(envVariable.Name, "Failed to validate environment variable: " + e.Message);
            }

            foreach (var section in envVariable.PopulateTo)
            {
                Data[section] = value;
            }
        }

        if (errors.Count != 0)
        {
            throw new InvalidEnvVariablesException("Found errors in environment variables", errors);
        }
    }

    public IEnumerable<EnvVariable> GetAll()
    {
        return _envVariables.Concat(_dynamicVariables?.Invoke() ?? []);
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
                var env = _envVariables.Single(x => EnvVariable.NameComparer.Equals(x, envVariable));
                env.MergeWith(envVariable);
            }
        }
    }
}
