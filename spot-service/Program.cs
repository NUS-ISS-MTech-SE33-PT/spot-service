using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<SpotRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains";
        await next.Invoke();
    });
}
var logger = app.Logger;
app.Use(async (context, next) =>
{
    var utcNow = DateTime.UtcNow.ToString("o");
    var method = context.Request.Method;
    var path = context.Request.Path;
    var headers = string.Join("; ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));

    logger.LogInformation("{UtcNow}\t{Method}\t{Path} | Headers: {Headers}",
        utcNow, method, path, headers);

    await next.Invoke();
});

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
app.MapGet("/spots/{id}", async (string id, [FromServices] SpotRepository repo) =>
{
    var spot = await repo.GetSpotByIdAsync(id);
    if (spot == null) return Results.NotFound();
    return Results.Ok(spot);
});

app.Run();

public partial class Program { }
