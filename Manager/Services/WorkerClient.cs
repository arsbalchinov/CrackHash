using Manager.Models;

namespace Manager.Services;

public class WorkerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkerClient> _logger;
    
    public WorkerClient(HttpClient httpClient, ILogger<WorkerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<List<string>> SendTaskAsync(string workerUrl, WorkerTask task)
    {
        try
        {
            _logger.LogDebug("Sending task to {WorkerUrl}: request {RequestId}, range [{StartIndex}, {EndIndex})", 
                workerUrl, task.RequestId, task.StartIndex, task.EndIndex);
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{workerUrl}/internal/api/worker/hash/crack/task", 
                task);
                
            response.EnsureSuccessStatusCode();
            
            // Проверяем, есть ли контент
            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Worker {WorkerUrl} returned empty response", workerUrl);
                return new List<string>();
            }
            
            // Пробуем десериализовать
            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<List<string>>(content);
                _logger.LogDebug("Worker {WorkerUrl} returned {Count} results", workerUrl, result?.Count ?? 0);
                return result ?? new List<string>();
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response from {WorkerUrl}. Content: {Content}", 
                    workerUrl, content);
                return new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task to {WorkerUrl}", workerUrl);
            return new List<string>();
        }
    }
}