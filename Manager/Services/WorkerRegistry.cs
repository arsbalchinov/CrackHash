using System.Collections.Concurrent;
using Manager.Models;

namespace Manager.Services;

public class WorkerRegistry
{
    private readonly ConcurrentDictionary<string, WorkerInfo> _workers = new();
    private readonly ILogger<WorkerRegistry> _logger;
    private readonly TimeSpan _heartbeatTimeout;
    
    public WorkerRegistry(IConfiguration config, ILogger<WorkerRegistry> logger)
    {
        _logger = logger;
        var timeoutSeconds = config.GetValue("HeartbeatTimeout", 30);
        _heartbeatTimeout = TimeSpan.FromSeconds(timeoutSeconds);
    }
    
    public string Register(string url)
    {
        var workerId = Guid.NewGuid().ToString();
        var worker = new WorkerInfo
        {
            WorkerId = workerId,
            Url = url,
            LastHeartbeat = DateTime.UtcNow,
            IsHealthy = true
        };
        
        _workers[workerId] = worker;
        _logger.LogInformation("Worker registered: {WorkerId} at {Url}", workerId, url);
        
        return workerId;
    }
    
    public bool Heartbeat(string workerId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.LastHeartbeat = DateTime.UtcNow;
            worker.IsHealthy = true;
            _logger.LogDebug("Heartbeat received from worker {WorkerId}", workerId);
            return true;
        }
        
        _logger.LogWarning("Heartbeat from unknown worker {WorkerId}", workerId);
        return false;
    }
    
    public List<WorkerInfo> GetHealthyWorkers()
    {
        return _workers.Values.Where(w => w.IsHealthy).ToList();
    }
    
    public List<WorkerInfo> GetDeadWorkers()
    {
        var now = DateTime.UtcNow;
        var deadWorkers = _workers.Values.Where(w => now - w.LastHeartbeat > _heartbeatTimeout).ToList();
        foreach (var worker in deadWorkers)
        {
            worker.IsHealthy = false;
        }
        return deadWorkers;
    }
    
    public void RemoveWorker(string workerId)
    {
        if (_workers.TryRemove(workerId, out var worker))
        {
            _logger.LogWarning("Worker removed: {WorkerId} (last heartbeat: {LastHeartbeat})", 
                worker.WorkerId, worker.LastHeartbeat);
        }
    }
    
    public void RemoveDeadWorkers()
    {
        var deadWorkers = GetDeadWorkers();
        
        foreach (var worker in deadWorkers)
        {
            RemoveWorker(worker.WorkerId);
        }
    }
    
    public void AssignTask(string workerId, Guid requestId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.ActiveTasks.Add(requestId);
            _logger.LogDebug("Task {RequestId} assigned to worker {WorkerId}", requestId, workerId);
        }
    }
    
    public void CompleteTask(string workerId, Guid requestId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.ActiveTasks.Remove(requestId);
            _logger.LogDebug("Task {RequestId} completed by worker {WorkerId}", requestId, workerId);
        }
    }
}