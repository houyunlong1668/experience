var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 首页：基本服务信息
app.MapGet("/", () => new
{
    service = "Experience.Api",
    description = "简易 .NET Web API，通过 GitHub Actions 部署到 Azure App Service",
    endpoints = new[] { "/api/hello", "/api/time", "/health", "/weatherforecast" }
})
.WithName("Root");

// 问候接口：GET /api/hello?name=xxx
app.MapGet("/api/hello", (string? name) => new
{
    message = $"Hello, {name ?? "World"}!",
    time = DateTime.UtcNow
})
.WithName("Hello");

// 时间接口：GET /api/time —— 用于验证线上跑的是动态进程
app.MapGet("/api/time", () => new
{
    utcNow = DateTime.UtcNow,
    localNow = DateTime.Now,
    machine = Environment.MachineName
})
.WithName("Time");

// 健康检查：GET /health —— 可配到 Azure App Service 的健康检查路径
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
.WithName("Health");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
