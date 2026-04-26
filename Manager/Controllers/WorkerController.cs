using Manager.Models;
using Manager.Services;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Controllers;

[ApiController]
[Route("api/workers")]
public class WorkerController : ControllerBase
{
    private readonly WorkerRegistry _registry;
    private readonly RequestStore _requestStore;
    private readonly ILogger<WorkerController> _logger;
    
    public WorkerController(
        WorkerRegistry registry, 
        RequestStore requestStore,
        ILogger<WorkerController> logger)
    {
        _registry = registry;
        _requestStore = requestStore;
        _logger = logger;
    }
    
    [HttpPost("register")]
    public IActionResult Register([FromBody] WorkerRegistrationRequest request)
    {
        var workerId = _registry.Register(request.Url);
        return Ok(new WorkerRegistrationResponse { WorkerId = workerId });
    }
    
    [HttpPost("heartbeat")]
    public IActionResult Heartbeat([FromBody] WorkerHeartbeatRequest request)
    {
        if (_registry.Heartbeat(request.WorkerId))
            return Ok();
            
        return NotFound();
    }
    
    [HttpPost("progress")]
    public IActionResult ReceiveProgress([FromBody] WorkerProgressRequest request)
    {
        if (!_requestStore.TryGet(request.RequestId, out var state))
            return NotFound();

        lock (state)
        {
            if (!state.WorkerProcessed.ContainsKey(request.WorkerId))
                state.WorkerProcessed[request.WorkerId] = 0;

            if (request.Processed > state.WorkerProcessed[request.WorkerId])
            {
                state.WorkerProcessed[request.WorkerId] = request.Processed;
                state.ProcessedCombinations = state.WorkerProcessed.Values.Sum();
                state.Progress = (int)(state.ProcessedCombinations * 100 / state.TotalCombinations);
            }
            
            if (request.Results != null && request.Results.Any())
            {
                foreach (var word in request.Results)
                    if (!state.Results.Contains(word))
                        state.Results.Add(word);
            }
        }
        return Ok();
    }
    
    [HttpPost("result")]
    public IActionResult ReceiveResult([FromBody] WorkerResultRequest request)
    {
        _logger.LogInformation("Received {Count} results from worker {WorkerId} for request {RequestId}",
            request.Results?.Count ?? 0, request.WorkerId, request.RequestId);
        
        if (!_requestStore.TryGet(request.RequestId, out var state))
        {
            _logger.LogWarning("Request {RequestId} not found", request.RequestId);
            return NotFound();
        }

        lock (state)
        {
            if (request.Results != null && request.Results.Any())
            {
                foreach (var word in request.Results)
                    if (!state.Results.Contains(word))
                        state.Results.Add(word);
            }

            state.CompletedParts++;

            if (state.CompletedParts >= state.TotalParts)
            {
                state.Status = "READY";
                state.Progress = 100;
                _logger.LogInformation("Request {RequestId} completed with {Count} results",
                    request.RequestId, state.Results.Count);
            }
            else
            {
                _logger.LogInformation("Request {RequestId} progress: {Progress}% ({Completed}/{Total})",
                    request.RequestId, state.Progress, state.CompletedParts, state.TotalParts);
            }
        }

        _registry.CompleteTask(request.WorkerId, request.RequestId);
        return Ok();
    }
}