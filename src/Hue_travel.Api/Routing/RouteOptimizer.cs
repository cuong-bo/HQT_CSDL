namespace Hue_travel.Api.Routing;

/// <summary>Best order to visit stops: exact (Held-Karp dynamic programming), for up to 12 stops.</summary>
public static class RouteOptimizer
{
    public const int MaxStops = 12;

    /// <summary>
    /// <paramref name="cost"/>[i, j] is the cost of going from place i to place j (index 0 is the start, 1..n are the
    /// stops; use infinity when there is no way). Returns the stop numbers (1..n) in the cheapest visiting order of an
    /// open path that begins at the start, or null when some stop cannot be reached.
    /// </summary>
    public static IReadOnlyList<int>? Order(double[,] cost)
    {
        int n = cost.GetLength(0) - 1;
        if (n <= 0) return [];
        if (n > MaxStops) throw new ArgumentException($"At most {MaxStops} stops.", nameof(cost));

        int full = 1 << n;
        var best = new double[full, n];
        var previous = new int[full, n];
        for (int mask = 0; mask < full; mask++)
            for (int j = 0; j < n; j++) best[mask, j] = double.PositiveInfinity;
        for (int j = 0; j < n; j++) best[1 << j, j] = cost[0, j + 1];

        for (int mask = 1; mask < full; mask++)
        {
            for (int last = 0; last < n; last++)
            {
                double here = best[mask, last];
                if ((mask & (1 << last)) == 0 || double.IsPositiveInfinity(here)) continue;
                for (int next = 0; next < n; next++)
                {
                    if ((mask & (1 << next)) != 0) continue;
                    double candidate = here + cost[last + 1, next + 1];
                    int target = mask | (1 << next);
                    if (candidate >= best[target, next]) continue;
                    best[target, next] = candidate;
                    previous[target, next] = last;
                }
            }
        }

        int end = -1;
        double bestTotal = double.PositiveInfinity;
        for (int j = 0; j < n; j++)
        {
            if (best[full - 1, j] >= bestTotal) continue;
            bestTotal = best[full - 1, j];
            end = j;
        }
        if (end < 0) return null;

        var order = new int[n];
        int remaining = full - 1;
        for (int position = n - 1; position >= 0; position--)
        {
            order[position] = end + 1;
            int before = previous[remaining, end];
            remaining &= ~(1 << end);
            end = before;
        }
        return order;
    }
}
