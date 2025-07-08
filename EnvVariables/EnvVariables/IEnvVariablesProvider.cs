namespace EnvVariables;

/// <summary>
/// Provider of <see cref="EnvVariable"/>.
/// </summary>
public interface IEnvVariablesProvider
{
    /// <summary>
    /// Adds env variables.
    /// </summary>
    /// <param name="envVariables">Env variables.</param>
    void Add(IReadOnlyCollection<EnvVariable> envVariables);

    /// <summary>
    /// Returns all registered variables.
    /// </summary>
    IEnumerable<EnvVariable> GetAll();
}
