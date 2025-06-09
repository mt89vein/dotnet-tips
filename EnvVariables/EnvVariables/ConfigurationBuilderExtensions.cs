namespace EnvVariables;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEnvSubstitution(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        params EnvVariable[] envVariables
    )
    {
        return builder.ConfigureCore(loggerFactory, envVariables);
    }

    public static IConfigurationBuilder AddEnvSubstitution(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> envVariables
    )
    {
        return builder.ConfigureCore(loggerFactory, envVariables);
    }

    private static IConfigurationBuilder ConfigureCore(
        this IConfigurationBuilder builder,
        ILoggerFactory loggerFactory,
        IReadOnlyCollection<EnvVariable> envVariables
    )
    {
        var source = new EnvSubstitutionConfigurationSource(
            loggerFactory,
            envVariables
        );

        var args = Environment.GetCommandLineArgs();

        if (args.Contains("print-envs"))
        {
            source.PrintEnvs();

            Environment.Exit(0);
        }

        return builder.Add(source);
    }
}
