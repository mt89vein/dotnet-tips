namespace EnvVariables;

public static class ConfigurationBuilderExtensions
{
    public static EnvSubstitutionConfigurationProvider AddEnvSubstitution(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        Func<IEnumerable<EnvVariable>>? dynamicVariables = null,
        params EnvVariable[] envVariables
    )
    {
        return builder.ConfigureCore(loggerFactory, envVariables, dynamicVariables);
    }

    public static EnvSubstitutionConfigurationProvider AddEnvSubstitution(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> envVariables,
        Func<IEnumerable<EnvVariable>>? dynamicVariables = null
    )
    {
        return builder.ConfigureCore(loggerFactory, envVariables, dynamicVariables);
    }

    private static EnvSubstitutionConfigurationProvider ConfigureCore(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> envVariables,
        Func<IEnumerable<EnvVariable>>? dynamicVariables = null
    )
    {
        var source = new EnvSubstitutionConfigurationProvider(
            envVariables,
            loggerFactory.CreateLogger<EnvSubstitutionConfigurationProvider>(),
            dynamicVariables
        );

        builder.Add(source);

        return source;
    }
}
