public interface ISpotRepository
{
    Task<List<Spot>> GetAllSpotsAsync();
    Task<Spot?> GetSpotByIdAsync(string id);
}

