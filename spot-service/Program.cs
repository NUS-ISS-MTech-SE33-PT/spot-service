using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<SpotRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

IResult ApiError(HttpContext context, int statusCode, string code, string message)
{
    return Results.Json(new
    {
        code,
        message,
        traceId = context.TraceIdentifier
    }, statusCode: statusCode);
}

static string SanitizeHeaders(IHeaderDictionary headers)
{
    var maskedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "x-user-sub"
    };

    return string.Join("; ", headers.Select(header =>
    {
        var value = maskedHeaders.Contains(header.Key) ? "***" : header.Value.ToString();
        return $"{header.Key}: {value}";
    }));
}

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        app.Logger.LogError(exception, "Unhandled exception while processing request.");

        var message = app.Environment.IsDevelopment()
            ? (exception?.Message ?? "Unhandled server error.")
            : "An unexpected error occurred.";

        var result = ApiError(context, StatusCodes.Status500InternalServerError, "internal_error", message);
        await result.ExecuteAsync(context);
    });
});

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
    var headers = SanitizeHeaders(context.Request.Headers);

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
app.MapGet("/spots/{id}", async (string id, HttpContext ctx, [FromServices] SpotRepository repo) =>
{
    var spot = await repo.GetSpotByIdAsync(id);
    if (spot == null)
    {
        return ApiError(ctx, StatusCodes.Status404NotFound, "not_found", "Spot not found.");
    }
    return Results.Ok(spot);
});

app.Run();

public partial class Program { }
