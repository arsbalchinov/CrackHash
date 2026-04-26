namespace Manager.Models;

public class WorkerInfo
{
    public string WorkerId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public bool IsHealthy { get; set; } = true;
    public List<Guid> ActiveTasks { get; set; } = new();
}

public class WorkerRegistrationRequest
{
    public string Url { get; set; } = string.Empty;
}

public class WorkerRegistrationResponse
{
    public string WorkerId { get; set; } = string.Empty;
}

public class WorkerHeartbeatRequest
{
    public string WorkerId { get; set; } = string.Empty;
}

public class WorkerProgressRequest
{
    public string WorkerId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public long Processed { get; set; }
    public List<string>? Results { get; set; }
}

public class WorkerResultRequest
{
    public string WorkerId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public List<string> Results { get; set; } = new();
}

public class WorkerTask
{
    public Guid RequestId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public string Alphabet { get; set; } = string.Empty;
    public long StartIndex { get; set; }
    public long EndIndex { get; set; }
    public int WorkerId { get; set; }
}