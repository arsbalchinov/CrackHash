namespace Worker.Services;

public class HeartbeatSender : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<HeartbeatSender> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeSpan _interval;
    
    public HeartbeatSender(
        IConfiguration config, 
        ILogger<HeartbeatSender> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _interval = TimeSpan.FromSeconds(_config.GetValue("HeartbeatInterval", 5));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var managerUrl = _config["ManagerUrl"] ?? "http://manager:8080";
        
        // Ждем регистрации
        while (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WORKER_ID")) && 
               !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
        
        var workerId = Environment.GetEnvironmentVariable("WORKER_ID");
        using var client = _httpClientFactory.CreateClient();
        
        _logger.LogInformation("Starting heartbeat sender for worker {WorkerId}, interval={Interval}s", 
            workerId, _interval.TotalSeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await client.PostAsJsonAsync(
                    $"{managerUrl}/api/workers/heartbeat",
                    new { WorkerId = workerId },
                    stoppingToken);
                    
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Heartbeat sent successfully");
                }
                else
                {
                    _logger.LogWarning("Heartbeat failed with status {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
    }
}