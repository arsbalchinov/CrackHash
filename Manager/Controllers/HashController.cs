using Manager.Models;
using Manager.Services;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Controllers;

[ApiController]
[Route("api/hash")]
public class HashController : ControllerBase
{
    private readonly RequestStore _store;
    private readonly WorkerRegistry _workerRegistry;
    private readonly ILogger<HashController> _logger;
    private readonly IConfiguration _config;
    private readonly WorkerClient _workerClient;
    
    public HashController(
        RequestStore store,
        WorkerRegistry workerRegistry,
        ILogger<HashController> logger,
        IConfiguration config,
        WorkerClient workerClient)
    {
        _store = store;
        _workerRegistry = workerRegistry;
        _logger = logger;
        _config = config;
        _workerClient = workerClient;
    }
    
    [HttpPost("crack")]
    public async Task<ActionResult<CrackHashResponse>> CrackHash([FromBody] CrackHashRequest request)
    {
        var healthyWorkers = _workerRegistry.GetHealthyWorkers();
        
        if (healthyWorkers.Count == 0)
        {
            _logger.LogError("No healthy workers available");
            return StatusCode(503, new { error = "No workers available" });
        }
        
        var requestId = Guid.NewGuid();
        
        var state = new CrackRequestState
        {
            RequestId = requestId,
            Hash = request.Hash,
            MaxLength = request.MaxLength,
            TotalParts = healthyWorkers.Count
        };
        
        _store.Add(state);
        _logger.LogInformation("Request {RequestId}: created for hash {Hash}, maxLength={MaxLength}, workers={Count}", 
            requestId, request.Hash, request.MaxLength, healthyWorkers.Count);
        
        // Запускаем обработку в фоне
        _ = Task.Run(() => ProcessRequest(state, healthyWorkers));
        
        return Ok(new CrackHashResponse { RequestId = requestId });
    }
    
    [HttpGet("status")]
    public ActionResult<CrackStatusResponse> GetStatus([FromQuery] Guid requestId)
    {
        if (!_store.TryGet(requestId, out var state))
            return NotFound();
            
        return Ok(new CrackStatusResponse
        {
            Status = state.Status,
            Progress = state.Progress,
            Data = state.Results
        });
    }
    
    private async Task ProcessRequest(CrackRequestState state, List<WorkerInfo> workers)
    {
        try
        {
            var alphabet = _config["Alphabet"] ?? "abcdefghijklmnopqrstuvwxyz0123456789";
            var totalCombinations = TaskSplitter.GetTotalCombinations(alphabet, state.MaxLength);
            state.TotalCombinations = totalCombinations;
            var chunkSize = totalCombinations / workers.Count;
            
            _logger.LogInformation("Request {RequestId}: total combinations = {Total:N0}, chunk size = {ChunkSize:N0}", 
                state.RequestId, totalCombinations, chunkSize);
            
            for (int i = 0; i < workers.Count; i++)
            {
                var startIndex = i * chunkSize;
                var endIndex = i == workers.Count - 1 ? totalCombinations : (i + 1) * chunkSize;
                
                var workerTask = new WorkerTask
                {
                    RequestId = state.RequestId,
                    Hash = state.Hash,
                    Alphabet = alphabet,
                    MaxLength = state.MaxLength,
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    WorkerId = i
                };
                
                _workerRegistry.AssignTask(workers[i].WorkerId, state.RequestId);
                
                _logger.LogInformation("Request {RequestId}: sending to worker {WorkerId} ({WorkerUrl}) range [{StartIndex}, {EndIndex}) = {Count:N0} combinations",
                    state.RequestId, workers[i].WorkerId, workers[i].Url, startIndex, endIndex, endIndex - startIndex);
                
                _ = _workerClient.SendTaskAsync(workers[i].Url, workerTask);
            }
            _logger.LogInformation("Request {RequestId}: all tasks sent to workers", state.RequestId);
        }
        catch (Exception ex)
        {
            state.Status = "ERROR";
            _logger.LogError(ex, "Request {RequestId}: failed", state.RequestId);
        }
    }
}