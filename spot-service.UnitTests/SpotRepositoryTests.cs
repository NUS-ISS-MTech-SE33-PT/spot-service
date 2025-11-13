using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

[TestFixture]
public class SpotRepositoryTests
{
    private Mock<IAmazonDynamoDB> _mockDynamoDb;
    private Mock<IConfiguration> _mockConfiguration;
    private SpotRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _mockDynamoDb = new Mock<IAmazonDynamoDB>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["DynamoDb"]).Returns("TestTable");

        _repository = new SpotRepository(_mockDynamoDb.Object, _mockConfiguration.Object);
    }

    [Test]
    public async Task GetAllSpotsAsync_ShouldReturnMappedSpots()
    {
        // Arrange
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "1" },
            ["name"] = new AttributeValue { S = "Spot A" },
            ["address"] = new AttributeValue { S = "Address A" },
            ["foodType"] = new AttributeValue { S = "Italian" },
            ["rating"] = new AttributeValue { N = "4.5" },
            ["openingHours"] = new AttributeValue { S = "9am - 9pm" },
            ["photos"] = new AttributeValue { L = new List<AttributeValue> { new AttributeValue { S = "photo1.jpg" } } },
            ["placeType"] = new AttributeValue { S = "Restaurant" },
            ["open"] = new AttributeValue { BOOL = true },
            ["isCenter"] = new AttributeValue { BOOL = false },
            ["latitude"] = new AttributeValue { N = "1.23" },
            ["longitude"] = new AttributeValue { N = "4.56" },
            ["parentCenter"] = new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue { S = "C1" },
                    ["name"] = new AttributeValue { S = "Center 1" },
                    ["thumbnailUrl"] = new AttributeValue { S = "thumb.jpg" }
                }
            }
        };

        _mockDynamoDb
            .Setup(db => db.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScanResponse
            {
                Items = new List<Dictionary<string, AttributeValue>> { item }
            });

        // Act
        var result = await _repository.GetAllSpotsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var spot = result.First();
        Assert.That(spot.Id, Is.EqualTo("1"));
        Assert.That(spot.Name, Is.EqualTo("Spot A"));
        Assert.That(spot.ParentCenter!.Id, Is.EqualTo("C1"));
        _mockDynamoDb.Verify(db => db.ScanAsync(It.Is<ScanRequest>(r => r.TableName == "TestTable"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetSpotByIdAsync_ShouldReturnSingleSpot_WhenExists()
    {
        // Arrange
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = "2" },
            ["name"] = new AttributeValue { S = "Spot B" },
            ["address"] = new AttributeValue { S = "Address B" },
            ["foodType"] = new AttributeValue { S = "Japanese" },
            ["rating"] = new AttributeValue { N = "3.8" },
            ["openingHours"] = new AttributeValue { S = "10am - 10pm" },
            ["photos"] = new AttributeValue { L = new List<AttributeValue> { new AttributeValue { S = "photoB.jpg" } } },
            ["placeType"] = new AttributeValue { S = "Cafe" },
            ["open"] = new AttributeValue { BOOL = false },
            ["isCenter"] = new AttributeValue { BOOL = true }
        };

        _mockDynamoDb
            .Setup(db => db.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse
            {
                Items = new List<Dictionary<string, AttributeValue>> { item }
            });

        // Act
        var result = await _repository.GetSpotByIdAsync("2");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("2"));
        Assert.That(result.Name, Is.EqualTo("Spot B"));
        _mockDynamoDb.Verify(db => db.QueryAsync(
            It.Is<QueryRequest>(r =>
                r.TableName == "TestTable" &&
                r.ExpressionAttributeValues[":id"].S == "2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetSpotByIdAsync_ShouldReturnNull_WhenNoResults()
    {
        // Arrange
        _mockDynamoDb
            .Setup(db => db.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse
            {
                Items = new List<Dictionary<string, AttributeValue>>() // empty
            });

        // Act
        var result = await _repository.GetSpotByIdAsync("999");

        // Assert
        Assert.That(result, Is.Null);
    }
}
