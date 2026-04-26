using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Worker.Models;
using Worker.Services;

namespace Worker.Controllers;

[ApiController]
[Route("internal/api/worker/hash/crack")]
public class TaskController : ControllerBase
{
    private readonly ILogger<TaskController> _logger;
    private readonly WordGenerator _generator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    
    public TaskController(
        ILogger<TaskController> logger, 
        WordGenerator generator,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _logger = logger;
        _generator = generator;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }
    
    [HttpPost("task")]
    public async Task<ActionResult<List<string>>> ExecuteTask([FromBody] WorkerTask task)
    {
        var workerId = Environment.GetEnvironmentVariable("WORKER_ID") ?? "unknown";

        _logger.LogInformation("Worker {WorkerId}: received task for request {RequestId}, range [{StartIndex}, {EndIndex})",
            workerId, task.RequestId, task.StartIndex, task.EndIndex);

        var results = new List<string>();
        var total = task.EndIndex - task.StartIndex;
        var processed = 0L;
        
        const long progressStep = 100000;
        var nextReport = progressStep;

        try
        {
            for (var i = task.StartIndex; i < task.EndIndex; i++)
            {
                var word = _generator.GetWordByIndex(i);
                var hash = ComputeMd5(word);
            
                if (string.Equals(hash, task.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(word);
                    _logger.LogInformation("Worker {WorkerId}: found match! '{Word}' -> {Hash}", workerId, word, hash);
                    await SendProgress(task.RequestId, workerId, processed + 1, new List<string> { word });
                }
                if (processed++ >= nextReport)
                {
                    await SendProgress(task.RequestId, workerId, processed, null);
                    nextReport += progressStep;
                }
            }

            _logger.LogInformation("Worker {WorkerId}: completed task for request {RequestId}, processed {Processed:N0}/{Total:N0} combinations, found {Count} matches",
                workerId, task.RequestId, processed, total, results.Count);
            
            await SendResults(task.RequestId, workerId, results);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker {WorkerId}: error processing task for request {RequestId}", 
                workerId, task.RequestId);
            return StatusCode(500, new List<string>());
        }
    }
    
    private async Task SendProgress(Guid requestId, string workerId, long processed, List<string>? newWords)
    {
        var managerUrl = _config["ManagerUrl"] ?? "http://manager:8080";
        using var client = _httpClientFactory.CreateClient();
        var payload = new
        {
            WorkerId = workerId,
            RequestId = requestId,
            Processed = processed,
            Results = newWords
        };
        try
        {
            var response = await client.PostAsJsonAsync($"{managerUrl}/api/workers/progress", payload);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Failed to send progress, status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending progress");
        }
    }
    
    private async Task SendResults(Guid requestId, string workerId, List<string> results)
    {
        var managerUrl = _config["ManagerUrl"] ?? "http://manager:8080";
        using var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.PostAsJsonAsync(
                $"{managerUrl}/api/workers/result",
                new
                {
                    WorkerId = workerId,
                    RequestId = requestId,
                    Results = results
                });
                
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Worker {WorkerId}: sent {Count} results to manager", workerId, results.Count);
            }
            else
            {
                _logger.LogWarning("Worker {WorkerId}: failed to send results", workerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker {WorkerId}: error sending results", workerId);
        }
    }
    
    private string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}