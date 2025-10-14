using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public class SpotSearchOptions
{
    private SpotSearchOptions()
    {
    }

    public string? Query { get; init; }
    public string? Cuisine { get; init; }
    public double? PriceMin { get; init; }
    public double? PriceMax { get; init; }
    public bool? OpenNow { get; init; }
    public bool? CentersOnly { get; init; }
    public string? Sort { get; init; }
    public double? UserLatitude { get; init; }
    public double? UserLongitude { get; init; }
    public IImmutableSet<string> QuickFilters { get; init; } = ImmutableHashSet<string>.Empty;

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public static SpotSearchOptions FromParameters(
        string? query,
        IEnumerable<string>? quickFilters,
        string? cuisine,
        double? priceMin,
        double? priceMax,
        bool? openNow,
        bool? centersOnly,
        string? sort,
        double? userLatitude,
        double? userLongitude)
    {
        var quickSet = ImmutableHashSet.Create<string>(Comparer, Array.Empty<string>());
        if (quickFilters != null)
        {
            var builder = ImmutableHashSet.CreateBuilder<string>(Comparer);
            foreach (var quick in quickFilters)
            {
                if (string.IsNullOrWhiteSpace(quick)) continue;
                var parts = quick.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        builder.Add(part.ToLowerInvariant());
                    }
                }
            }

            quickSet = builder.ToImmutable();
        }

        var normalizedSort = NormalizeSort(sort, quickSet);

        var options = new SpotSearchOptions
        {
            Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
            Cuisine = string.IsNullOrWhiteSpace(cuisine) ? null : cuisine.Trim(),
            PriceMin = priceMin,
            PriceMax = priceMax,
            OpenNow = openNow ?? (quickSet.Contains("open_now") ? true : null),
            CentersOnly = centersOnly ?? (quickSet.Contains("food_center") ? true : null),
            Sort = normalizedSort,
            UserLatitude = userLatitude,
            UserLongitude = userLongitude,
            QuickFilters = quickSet
        };

        return options;
    }

    private static string? NormalizeSort(string? sort, IImmutableSet<string> quickFilters)
    {
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var candidate = sort.Trim().ToLowerInvariant();
            if (candidate is "rating_desc" or "rating_asc" or "distance_asc" or "name_asc" or "name_desc")
            {
                return candidate;
            }
        }

        if (quickFilters.Contains("nearby"))
        {
            return "distance_asc";
        }

        if (quickFilters.Contains("top_rated") || quickFilters.Contains("taste_high"))
        {
            return "rating_desc";
        }

        return null;
    }
}
