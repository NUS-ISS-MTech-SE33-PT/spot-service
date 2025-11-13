using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class SpotSearchHelperTests
{
    private List<Spot> GetSampleSpots() => new()
    {
        new Spot
        {
            Name = "Coffee Bar",
            Address = "123 Bean St",
            FoodType = "Coffee",
            PlaceType = "Cafe",
            Rating = 4.6,
            AvgPrice = 10,
            Open = true,
            Latitude = 1.3,
            Longitude = 103.8,
            TasteAvg = 4.7,
            EnvironmentAvg = 4.3
        },
        new Spot
        {
            Name = "Luxury Bar",
            Address = "456 High St",
            FoodType = "Bar",
            PlaceType = "Pub",
            Rating = 4.9,
            AvgPrice = 25,
            Open = false,
            Latitude = 1.4,
            Longitude = 103.9,
            TasteAvg = 4.8,
            EnvironmentAvg = 4.5
        },
        new Spot
        {
            Name = "Food Center",
            Address = "Central Plaza",
            FoodType = "Mixed",
            PlaceType = "Food Center",
            Rating = 4.2,
            IsCenter = true,
            AvgPrice = 8,
            Open = true,
            Latitude = 1.35,
            Longitude = 103.85
        }
    };

    [Test]
    public void Apply_ShouldReturnEmpty_WhenSpotsIsNull()
    {
        var result = SpotSearchHelper.Apply(null, null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Apply_ShouldReturnAll_WhenOptionsIsNull()
    {
        var spots = GetSampleSpots();
        var result = SpotSearchHelper.Apply(spots, null);
        Assert.That(result.Count, Is.EqualTo(spots.Count));
    }

    [Test]
    public void Apply_ShouldFilterByQuery()
    {
        var spots = GetSampleSpots();
        var options = SpotSearchOptions.FromParameters(
            query: "Coffee",
            quickFilters: null,
            cuisine: null,
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: null,
            userLatitude: null,
            userLongitude: null
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Coffee Bar"));
    }

    [Test]
    public void Apply_ShouldFilterByCuisine()
    {
        var spots = GetSampleSpots();
        var options = SpotSearchOptions.FromParameters(
            query: null,
            quickFilters: null,
            cuisine: "Coffee",
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: null,
            userLatitude: null,
            userLongitude: null
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Coffee Bar"));
    }

    [Test]
    public void Apply_ShouldFilterOpenNow()
    {
        var spots = GetSampleSpots();
        var quickFilters = ImmutableHashSet.Create("open_now");
        var options = SpotSearchOptions.FromParameters(
            query: null,
            quickFilters: quickFilters,
            cuisine: null,
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: null,
            userLatitude: null,
            userLongitude: null
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result.All(s => s.Open), Is.True);
    }

    [Test]
    public void Apply_ShouldFilterCentersOnly()
    {
        var spots = GetSampleSpots();
        var quickFilters = ImmutableHashSet.Create("food_center");
        var options = SpotSearchOptions.FromParameters(
            query: null,
            quickFilters: quickFilters,
            cuisine: null,
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: null,
            userLatitude: null,
            userLongitude: null
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].IsCenter, Is.True);
    }

    [Test]
    public void Apply_ShouldFilterBudgetSpots()
    {
        var spots = GetSampleSpots();
        var quickFilters = ImmutableHashSet.Create("budget");
        var options = SpotSearchOptions.FromParameters(
            query: null,
            quickFilters: quickFilters,
            cuisine: null,
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: null,
            userLatitude: null,
            userLongitude: null
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result.All(s => s.IsCenter || s.AvgPrice <= 12), Is.True);
    }

    [Test]
    public void Apply_ShouldSortByRatingDesc()
    {
        var spots = GetSampleSpots();
        var options = SpotSearchOptions.FromParameters(
            query: null,
            quickFilters: null,
            cuisine: null,
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: "rating_desc",
            userLatitude: null,
            userLongitude: null
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result.First().Name, Is.EqualTo("Luxury Bar"));
    }

    [Test]
    public void Apply_ShouldSortByDistanceAsc()
    {
        var spots = GetSampleSpots();
        var options = SpotSearchOptions.FromParameters(
            query: null,
            quickFilters: null,
            cuisine: null,
            priceMin: null,
            priceMax: null,
            openNow: null,
            centersOnly: null,
            sort: "distance_asc",
            userLatitude: 1.31,
            userLongitude: 103.81
        );

        var result = SpotSearchHelper.Apply(spots, options);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.First().Name, Is.EqualTo("Coffee Bar")); // Nearest to (1.31, 103.81)
    }
}
