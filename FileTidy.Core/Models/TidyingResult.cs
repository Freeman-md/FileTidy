namespace FileTidy.Core.Models;

public class TidyingResult
{
    public int TotalFiles { get; set; }
    public int TotalMoved { get; set; }
    public int TotalErrors { get; set; }
    public Dictionary<string, int> PerCategoryCounts { get; set; } = new();
    public TimeSpan Elapsed { get; set; }
}
