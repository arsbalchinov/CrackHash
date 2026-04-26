namespace Manager.Services;

public class HeartbeatMonitor : BackgroundService
{
    private readonly WorkerRegistry _registry;
    private readonly RequestStore _requestStore;
    private readonly ILogger<HeartbeatMonitor> _logger;
    private readonly TimeSpan _checkInterval;
    
    public HeartbeatMonitor(WorkerRegistry registry, RequestStore requestStore, IConfiguration config, ILogger<HeartbeatMonitor> logger)
    {
        _registry = registry;
        _requestStore = requestStore;
        _logger = logger;   
        _checkInterval = TimeSpan.FromSeconds(config.GetValue("HeartbeatInterval", 10));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Heartbeat monitor starting, initial delay: {Delay}s", _checkInterval.TotalSeconds);
        await Task.Delay(_checkInterval, stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var deadWorkers = _registry.GetDeadWorkers();
                foreach (var worker in deadWorkers)
                {
                    foreach (var taskId in worker.ActiveTasks)
                    {
                        if (_requestStore.TryGet(taskId, out var state))
                        {
                            lock (state)
                            {
                                if (state.Status != "READY" && state.Status != "ERROR")
                                {
                                    state.Status = "ERROR";
                                    _logger.LogError("Task {TaskId} marked ERROR because worker {WorkerId} died", taskId, worker.WorkerId);
                                }
                            }
                        }
                    }
                    _registry.RemoveWorker(worker.WorkerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat monitor");
            }
            
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}