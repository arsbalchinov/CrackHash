namespace Worker.Services;

public class WorkerRegistrar : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WorkerRegistrar> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private string? _workerId;
    
    public WorkerRegistrar(
        IConfiguration config, 
        ILogger<WorkerRegistrar> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var managerUrl = _config["ManagerUrl"] ?? "http://manager:8080";
        var workerPort = _config["Port"] ?? "8080";
        var workerUrl = $"http://{GetHostName()}:{workerPort}";
        
        using var client = _httpClientFactory.CreateClient();
        
        while (!stoppingToken.IsCancellationRequested && string.IsNullOrEmpty(_workerId))
        {
            try
            {
                _logger.LogInformation("Registering with manager at {ManagerUrl}", managerUrl);
                
                var response = await client.PostAsJsonAsync(
                    $"{managerUrl}/api/workers/register",
                    new { Url = workerUrl }, 
                    stoppingToken);
                    
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RegistrationResponse>(cancellationToken: stoppingToken);
                    _workerId = result?.WorkerId;
                    
                    _logger.LogInformation("Successfully registered with manager. Worker ID: {WorkerId}", _workerId);
                    
                    // Сохраняем ID для heartbeat
                    Environment.SetEnvironmentVariable("WORKER_ID", _workerId);
                    break;
                }
                
                _logger.LogWarning("Registration failed with status {StatusCode}, retrying in 2 seconds...", 
                    response.StatusCode);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering with manager");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
    
    private string GetHostName()
    {
        try
        {
            return System.Net.Dns.GetHostName();
        }
        catch
        {
            return "localhost";
        }
    }
    
    private class RegistrationResponse
    {
        public string WorkerId { get; set; } = string.Empty;
    }
}