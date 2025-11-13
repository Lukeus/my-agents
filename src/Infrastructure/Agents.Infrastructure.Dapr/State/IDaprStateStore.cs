namespace Agents.Infrastructure.Dapr.State;

/// <summary>
/// Interface for Dapr state store operations
/// </summary>
public interface IDaprStateStore
{
    /// <summary>
    /// Gets state from the Dapr state store
    /// </summary>
    /// <typeparam name="T">Type of the state value</typeparam>
    /// <param name="key">The state key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The state value or null if not found</returns>
    Task<T?> GetStateAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves state to the Dapr state store
    /// </summary>
    /// <typeparam name="T">Type of the state value</typeparam>
    /// <param name="key">The state key</param>
    /// <param name="value">The state value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveStateAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes state from the Dapr state store
    /// </summary>
    /// <param name="key">The state key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteStateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a state key exists
    /// </summary>
    /// <param name="key">The state key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple states in a single operation
    /// </summary>
    /// <typeparam name="T">Type of the state values</typeparam>
    /// <param name="keys">The state keys</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of keys and their values</returns>
    Task<IDictionary<string, T?>> GetBulkStateAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
