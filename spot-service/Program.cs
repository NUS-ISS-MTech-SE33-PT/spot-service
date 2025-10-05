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
app.MapGet("/spots", async ([FromServices] SpotRepository repo) =>
{
    var spots = await repo.GetAllSpotsAsync();
    return Results.Ok(new GetSpotsResponse { Items = spots });
});

// GET /spots/{id}
app.MapGet("/spots/{id}", async (string id, [FromServices] SpotRepository repo) =>
{
    var spot = await repo.GetSpotByIdAsync(id);
    if (spot == null) return Results.NotFound();
    return Results.Ok(spot);
});

app.Run();
