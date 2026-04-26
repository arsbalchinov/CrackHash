namespace Worker.Models;

public class WorkerTask
{
    public Guid RequestId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public string Alphabet { get; set; } = "abcdefghijklmnopqrstuvwxyz0123456789";
    public long StartIndex { get; set; }
    public long EndIndex { get; set; }
    public int WorkerId { get; set; }
}