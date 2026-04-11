using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

[TestFixture]
public class ProgramTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private Mock<ISpotRepository> _repoMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<ISpotRepository>();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddScoped(_ => _repoMock.Object);
                });
            });
    }

    [TearDown]
    public void Teardown()
    {
        _factory.Dispose(); // Dispose to free resources
    }

    [Test]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/spots/health");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var date = await response.Content.ReadFromJsonAsync<DateTime>();
        Assert.That(date, Is.Not.EqualTo(default(DateTime)));
    }

    private sealed class GetSpotsResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public List<Spot> Items { get; set; } = new();
    }

    [Test]
    public async Task SpotsEndpoint_PageSizeAboveMax_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/spots?pageSize=9999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SpotsEndpoint_DefaultPagination_ReturnsDefaultPageSize()
    {
        _repoMock
            .Setup(r => r.GetAllSpotsAsync())
            .ReturnsAsync(Enumerable.Range(1, 60).Select(i => new Spot
            {
                Id = i.ToString(),
                Name = $"Spot {i}",
                Address = "Address",
                FoodType = "Food",
                Rating = 4.0,
                OpeningHours = "9-5",
                Photos = new List<string>(),
                PlaceType = "Restaurant",
                Open = true,
                IsCenter = false
            }).ToList());

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/spots");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<GetSpotsResponseDto>();
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Page, Is.EqualTo(1));
        Assert.That(payload.PageSize, Is.EqualTo(50));
        Assert.That(payload.TotalItems, Is.EqualTo(60));
        Assert.That(payload.Items, Has.Count.EqualTo(50));
    }

    [Test]
    public async Task SpotsEndpoint_Page2_ReturnsRemainingItems()
    {
        _repoMock
            .Setup(r => r.GetAllSpotsAsync())
            .ReturnsAsync(Enumerable.Range(1, 60).Select(i => new Spot
            {
                Id = i.ToString(),
                Name = $"Spot {i}",
                Address = "Address",
                FoodType = "Food",
                Rating = 4.0,
                OpeningHours = "9-5",
                Photos = new List<string>(),
                PlaceType = "Restaurant",
                Open = true,
                IsCenter = false
            }).ToList());

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/spots?page=2&pageSize=50");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<GetSpotsResponseDto>();
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Page, Is.EqualTo(2));
        Assert.That(payload.PageSize, Is.EqualTo(50));
        Assert.That(payload.TotalItems, Is.EqualTo(60));
        Assert.That(payload.Items, Has.Count.EqualTo(10));
    }
}
