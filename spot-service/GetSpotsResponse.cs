public class GetSpotsResponse
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalItems { get; set; }
    public IEnumerable<Spot> Items { get; set; } = new List<Spot>();
}
