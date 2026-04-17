using Amazon.DynamoDBv2;
using MakanGo.Common;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<SpotRepository>();

var app = builder.Build();

app.UseApiExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHstsInProduction();
app.UseRequestLogging();

// GET /spots/health
app.MapGet("/spots/health", () => Results.Ok(DateTime.Now));

// GET /spots
app.MapGet("/spots", async (
    [FromServices] SpotRepository repo,
    [FromQuery] string? query,
    [FromQuery(Name = "quick")] string[]? quickFilters,
    [FromQuery] string? cuisine,
    [FromQuery] double? priceMin,
    [FromQuery] double? priceMax,
    [FromQuery] bool? openNow,
    [FromQuery] bool? centersOnly,
    [FromQuery] string? sort,
    [FromQuery] double? userLatitude,
    [FromQuery] double? userLongitude) =>
{
    var options = SpotSearchOptions.FromParameters(
        query,
        quickFilters,
        cuisine,
        priceMin,
        priceMax,
        openNow,
        centersOnly,
        sort,
        userLatitude,
        userLongitude);

    var spots = await repo.GetAllSpotsAsync();
    var filtered = SpotSearchHelper.Apply(spots, options);
    return Results.Ok(new GetSpotsResponse { Items = filtered });
});

// GET /spots/{id}
app.MapGet("/spots/{id}", async (string id, HttpContext ctx, [FromServices] SpotRepository repo) =>
{
    var spot = await repo.GetSpotByIdAsync(id);
    if (spot == null)
    {
        return ApiErrors.Create(ctx, StatusCodes.Status404NotFound, "not_found", "Spot not found.");
    }
    return Results.Ok(spot);
});

app.Run();

public partial class Program { }
