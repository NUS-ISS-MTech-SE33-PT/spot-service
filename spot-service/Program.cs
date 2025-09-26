var builder = WebApplication.CreateBuilder(args);
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
    logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next.Invoke();
});
app.MapGet("/health", () => Results.Ok(DateTime.Now));
app.MapGet("/", () => "Hello World from spot service!");
app.Run();
