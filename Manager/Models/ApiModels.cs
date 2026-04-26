namespace Manager.Models;

// Клиентские модели
public class CrackHashRequest
{
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
}

public class CrackHashResponse
{
    public Guid RequestId { get; set; }
}

public class CrackStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public List<string>? Data { get; set; }
}

// Внутренние модели
public class CrackRequestState
{
    public Guid RequestId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public int Progress { get; set; }
    public List<string> Results { get; set; } = new();
    public int TotalParts { get; set; }
    public int CompletedParts { get; set; }
    public long TotalCombinations { get; set; }
    public long ProcessedCombinations { get; set; }
    public Dictionary<string, long> WorkerProcessed { get; set; } = new();
}