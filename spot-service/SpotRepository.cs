using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

public class SpotRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public SpotRepository(IAmazonDynamoDB dynamoDb, IConfiguration configuration)
    {
        _dynamoDb = dynamoDb;
        _tableName = configuration["DynamoDb"]!;
    }

    public async Task<List<Spot>> GetAllSpotsAsync()
    {
        var scanRequest = new ScanRequest
        {
            TableName = _tableName
        };

        var response = await _dynamoDb.ScanAsync(scanRequest);
        var spots = response.Items.Select(item => new Spot()
        {
            Id = item["id"].S,
            Name = item["name"].S,
            Address = item["address"].S,
            FoodType = item["foodType"].S,
            Rating = double.Parse(item["rating"].N),
            OpeningHours = item["openingHours"].S,
            Photos = item["photos"].L.Select(p => p.S).ToList(),
            PlaceType = item["placeType"].S,
            Open = item["open"].BOOL!.Value,
            IsCenter = item["isCenter"].BOOL!.Value,

            AvgPrice = item.TryGetValue("avgPrice", out var avgPriceValue) ? double.Parse(avgPriceValue.N) : null,
            Latitude = item.TryGetValue("latitude", out var latitudeValue) ? double.Parse(latitudeValue.N) : 0,
            Longitude = item.TryGetValue("longitude", out var longitudeValue) ? double.Parse(longitudeValue.N) : 0,
            TasteAvg = item.TryGetValue("tasteAvg", out var tasteAvgValue) ? double.Parse(tasteAvgValue.N) : null,
            ServiceAvg = item.TryGetValue("serviceAvg", out var serviceAvgValue) ? double.Parse(serviceAvgValue.N) : null,
            EnvironmentAvg = item.TryGetValue("environmentAvg", out var environmentAvgValue) ? double.Parse(environmentAvgValue.N) : null,
            District = item.TryGetValue("district", out var districtValue) ? districtValue.S : null,
            ThumbnailUrl = item.TryGetValue("thumbnailUrl", out var thumbnailValue) ? thumbnailValue.S : null,
            ParentCenter = item.TryGetValue("parentCenter", out var parentCenterValue) ? new CenterSummary
            {
                Id = parentCenterValue.M["id"].S,
                Name = parentCenterValue.M["name"].S,
                ThumbnailUrl = parentCenterValue.M["thumbnailUrl"].S
            } : null
        }).ToList();
        return spots;
    }

    public async Task<Spot?> GetSpotByIdAsync(string id)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "id = :id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":id"] = new AttributeValue { S = id }
            }
        };

        var response = await _dynamoDb.QueryAsync(request);
        var spot = response.Items.Select(item => new Spot()
        {
            Id = item["id"].S,
            Name = item["name"].S,
            Address = item["address"].S,
            FoodType = item["foodType"].S,
            Rating = double.Parse(item["rating"].N),
            OpeningHours = item["openingHours"].S,
            Photos = item["photos"].L.Select(p => p.S).ToList(),
            PlaceType = item["placeType"].S,
            Open = item["open"].BOOL!.Value,
            IsCenter = item["isCenter"].BOOL!.Value,

            AvgPrice = item.TryGetValue("avgPrice", out var avgPriceValue) ? double.Parse(avgPriceValue.N) : null,
            Latitude = item.TryGetValue("latitude", out var latitudeValue) ? double.Parse(latitudeValue.N) : 0,
            Longitude = item.TryGetValue("longitude", out var longitudeValue) ? double.Parse(longitudeValue.N) : 0,
            TasteAvg = item.TryGetValue("tasteAvg", out var tasteAvgValue) ? double.Parse(tasteAvgValue.N) : null,
            ServiceAvg = item.TryGetValue("serviceAvg", out var serviceAvgValue) ? double.Parse(serviceAvgValue.N) : null,
            EnvironmentAvg = item.TryGetValue("environmentAvg", out var environmentAvgValue) ? double.Parse(environmentAvgValue.N) : null,
            District = item.TryGetValue("district", out var districtValue) ? districtValue.S : null,
            ThumbnailUrl = item.TryGetValue("thumbnailUrl", out var thumbnailValue) ? thumbnailValue.S : null,
            ParentCenter = item.TryGetValue("parentCenter", out var parentCenterValue) ? new CenterSummary
            {
                Id = parentCenterValue.M["id"].S,
                Name = parentCenterValue.M["name"].S,
                ThumbnailUrl = parentCenterValue.M["thumbnailUrl"].S
            } : null
        }).FirstOrDefault();
        return spot;
    }
}