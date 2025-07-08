namespace EnvVariables;

public static class ConfigurationBuilderExtensions
{
    public static IEnvVariablesProvider AddEnvVariables(
        this IConfigurationBuilder builder,
        IReadOnlyCollection<EnvVariable>? envVariables = null,
        Func<IEnumerable<EnvVariable>>? dynamicVariables = null
    )
    {
        var source = new EnvSubstitutionConfigurationProvider(
            envVariables ?? [],
            dynamicVariables
        );

        builder.Add(source);

        return source;
    }
}
