using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace Agents.Infrastructure.Dapr.State;

/// <summary>
/// Dapr-based implementation of state store for managing application state
/// </summary>
public class DaprStateStore : IDaprStateStore
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprStateStore> _logger;
    private const string StateStoreName = "agents-statestore";

    public DaprStateStore(DaprClient daprClient, ILogger<DaprStateStore> logger)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets state from the Dapr state store
    /// </summary>
    public async Task<T?> GetStateAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        try
        {
            _logger.LogDebug("Getting state for key: {Key}", key);

            var state = await _daprClient.GetStateAsync<T>(
                StateStoreName,
                key,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Successfully retrieved state for key: {Key}", key);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get state for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Saves state to the Dapr state store
    /// </summary>
    public async Task SaveStateAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        try
        {
            _logger.LogDebug("Saving state for key: {Key}", key);

            await _daprClient.SaveStateAsync(
                StateStoreName,
                key,
                value,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully saved state for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save state for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Deletes state from the Dapr state store
    /// </summary>
    public async Task DeleteStateAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        try
        {
            _logger.LogDebug("Deleting state for key: {Key}", key);

            await _daprClient.DeleteStateAsync(
                StateStoreName,
                key,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully deleted state for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete state for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Checks if a state key exists
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        try
        {
            var state = await _daprClient.GetStateAsync<object>(
                StateStoreName,
                key,
                cancellationToken: cancellationToken);

            return state != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets multiple states in a single operation
    /// </summary>
    public async Task<IDictionary<string, T?>> GetBulkStateAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        var keysList = keys.ToList();

        if (!keysList.Any())
        {
            return new Dictionary<string, T?>();
        }

        try
        {
            _logger.LogDebug("Getting bulk state for {Count} keys", keysList.Count);

            var result = await _daprClient.GetBulkStateAsync(
                StateStoreName,
                keysList,
                parallelism: 10,
                cancellationToken: cancellationToken);

            var dictionary = new Dictionary<string, T?>();
            foreach (var item in result)
            {
                dictionary[item.Key] = item.Value != null && !string.IsNullOrEmpty(item.Value)
                    ? System.Text.Json.JsonSerializer.Deserialize<T>(item.Value)
                    : default(T);
            }

            _logger.LogDebug("Successfully retrieved bulk state for {Count} keys", keysList.Count);
            return dictionary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bulk state for {Count} keys", keysList.Count);
            throw;
        }
    }
}
