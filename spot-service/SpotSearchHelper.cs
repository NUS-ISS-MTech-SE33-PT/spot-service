using System;
using System.Collections.Immutable;
using System.Linq;

public static class SpotSearchHelper
{
    private const double BudgetPriceThreshold = 12;
    private const double TasteHighThreshold = 4.5;
    private const double AmbienceHighThreshold = 4.2;
    private const double EarthRadiusKm = 6371.0;

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public static IReadOnlyList<Spot> Apply(IEnumerable<Spot> spots, SpotSearchOptions? options)
    {
        if (spots == null) return Array.Empty<Spot>();
        var working = spots.ToList();
        if (options == null) return working;

        var quick = options.QuickFilters ?? ImmutableHashSet<string>.Empty;

        if (!string.IsNullOrWhiteSpace(options.Query))
        {
            var query = options.Query!.Trim();
            working = working.Where(spot =>
                Contains(spot.Name, query) ||
                Contains(spot.Address, query) ||
                Contains(spot.FoodType, query) ||
                Contains(spot.PlaceType, query)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(options.Cuisine))
        {
            var cuisine = options.Cuisine!.Trim();
            working = working
                .Where(spot => !spot.IsCenter && string.Equals(spot.FoodType, cuisine, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var openNow = options.OpenNow == true || quick.Contains("open_now");
        if (openNow)
        {
            working = working.Where(spot => spot.Open).ToList();
        }

        var centersOnly = options.CentersOnly == true || quick.Contains("food_center");
        if (centersOnly)
        {
            working = working.Where(spot => spot.IsCenter).ToList();
        }

        if (quick.Contains("coffee"))
        {
            working = working.Where(spot => MatchesKeyword(spot, "coffee")).ToList();
        }

        if (quick.Contains("dessert"))
        {
            working = working.Where(spot => MatchesKeyword(spot, "dessert") || MatchesKeyword(spot, "sweet")).ToList();
        }

        if (quick.Contains("bars"))
        {
            working = working.Where(spot => MatchesKeyword(spot, "bar") || MatchesKeyword(spot, "taproom")).ToList();
        }

        if (quick.Contains("budget"))
        {
            working = working.Where(spot => spot.IsCenter ||
                (spot.AvgPrice.HasValue && spot.AvgPrice.Value <= BudgetPriceThreshold)).ToList();
        }

        if (quick.Contains("taste_high"))
        {
            working = working.Where(spot => spot.TasteAvg.HasValue && spot.TasteAvg.Value >= TasteHighThreshold).ToList();
        }

        if (quick.Contains("ambience_high"))
        {
            working = working.Where(spot => spot.EnvironmentAvg.HasValue && spot.EnvironmentAvg.Value >= AmbienceHighThreshold).ToList();
        }

        if (options.PriceMin.HasValue || options.PriceMax.HasValue)
        {
            var min = options.PriceMin;
            var max = options.PriceMax;
            working = working.Where(spot =>
            {
                if (spot.IsCenter) return true;
                if (!spot.AvgPrice.HasValue) return true;
                var price = spot.AvgPrice.Value;
                if (min.HasValue && price < min.Value) return false;
                if (max.HasValue && price > max.Value) return false;
                return true;
            }).ToList();
        }

        var sortKey = options.Sort?.ToLowerInvariant();
        if (sortKey == "distance_asc")
        {
            if (options.UserLatitude.HasValue && options.UserLongitude.HasValue)
            {
                var lat = options.UserLatitude.Value;
                var lng = options.UserLongitude.Value;
                working = working
                    .Select(spot => new
                    {
                        Spot = spot,
                        Distance = ComputeDistanceKm(lat, lng, spot.Latitude, spot.Longitude)
                    })
                    .OrderBy(x => x.Distance ?? double.MaxValue)
                    .ThenBy(x => x.Spot.Name, Comparer)
                    .Select(x => x.Spot)
                    .ToList();
            }
            else
            {
                sortKey = "rating_desc";
            }
        }

        switch (sortKey)
        {
            case "rating_desc":
                working = working.OrderByDescending(spot => spot.Rating).ThenBy(spot => spot.Name, Comparer).ToList();
                break;
            case "rating_asc":
                working = working.OrderBy(spot => spot.Rating).ThenBy(spot => spot.Name, Comparer).ToList();
                break;
            case "name_desc":
                working = working.OrderByDescending(spot => spot.Name, Comparer).ToList();
                break;
            case "name_asc":
                working = working.OrderBy(spot => spot.Name, Comparer).ToList();
                break;
            default:
                if (sortKey is null)
                {
                    // Smart sort defaults to rating
                    working = working.OrderByDescending(spot => spot.Rating).ThenBy(spot => spot.Name, Comparer).ToList();
                }
                break;
        }

        return working;
    }

    private static bool Contains(string? source, string query)
    {
        if (string.IsNullOrWhiteSpace(source)) return false;
        return source.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesKeyword(Spot spot, string keyword)
    {
        return Contains(spot.FoodType, keyword) ||
               Contains(spot.PlaceType, keyword) ||
               Contains(spot.Name, keyword);
    }

    private static double? ComputeDistanceKm(double userLat, double userLng, double spotLat, double spotLng)
    {
        if (Math.Abs(spotLat) < double.Epsilon && Math.Abs(spotLng) < double.Epsilon)
        {
            return null;
        }

        double dLat = DegreesToRadians(spotLat - userLat);
        double dLon = DegreesToRadians(spotLng - userLng);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(DegreesToRadians(userLat)) *
                Math.Cos(DegreesToRadians(spotLat)) *
                Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
