namespace EnvVariables;

public static class ConfigurationBuilderExtensions
{
    public static EnvSubstitutionConfigurationProvider AddEnvSubstitution(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        params EnvVariable[] envVariables
    )
    {
        return builder.ConfigureCore(loggerFactory, envVariables);
    }

    public static EnvSubstitutionConfigurationProvider AddEnvSubstitution(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> envVariables
    )
    {
        return builder.ConfigureCore(loggerFactory, envVariables);
    }

    private static EnvSubstitutionConfigurationProvider ConfigureCore(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> envVariables
    )
    {
        var source = new EnvSubstitutionConfigurationProvider(
            envVariables,
            loggerFactory.CreateLogger<EnvSubstitutionConfigurationProvider>()
        );

        var args = Environment.GetCommandLineArgs();

        if (args.Contains("print-envs"))
        {
            source.PrintEnvs();

            Environment.Exit(0);
        }

        builder.Add(source);

        return source;
    }
}
