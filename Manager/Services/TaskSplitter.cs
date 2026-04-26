namespace Manager.Services;

/// <summary>
/// Разделяет общую задачу на подзадачи для воркеров
/// </summary>
public static class TaskSplitter
{
    /// <summary>
    /// Вычисляет общее количество комбинаций для заданной максимальной длины
    /// </summary>
    public static long GetTotalCombinations(string alphabet, int maxLength)
    {
        long total = 0;
        long current = 1;
        
        for (int i = 1; i <= maxLength; i++)
        {
            current *= alphabet.Length;
            total += current;
        }
        
        return total;
    }
    
    /// <summary>
    /// Разбивает пространство комбинаций на равные диапазоны для воркеров
    /// </summary>
    public static List<TaskRange> SplitIntoRanges(string alphabet, int maxLength, int workerCount)
    {
        var totalCombinations = GetTotalCombinations(alphabet, maxLength);
        var chunkSize = totalCombinations / workerCount;
        var ranges = new List<TaskRange>();
        
        for (int i = 0; i < workerCount; i++)
        {
            var startIndex = i * chunkSize;
            var endIndex = i == workerCount - 1 
                ? totalCombinations 
                : (i + 1) * chunkSize;
            
            ranges.Add(new TaskRange
            {
                WorkerId = i,
                StartIndex = startIndex,
                EndIndex = endIndex,
                CombinationsCount = endIndex - startIndex
            });
        }
        
        return ranges;
    }
}

/// <summary>
/// Представляет диапазон комбинаций для одного воркера
/// </summary>
public class TaskRange
{
    public int WorkerId { get; set; }
    public long StartIndex { get; set; }
    public long EndIndex { get; set; }
    public long CombinationsCount { get; set; }
}