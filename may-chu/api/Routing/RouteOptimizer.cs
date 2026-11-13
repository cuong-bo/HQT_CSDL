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

    /// <summary>
    /// The same exact search, but the state at a stop is the earliest moment the visit there can end, after waiting for the
    /// door to open and staying for the planned time. A stop whose opening hours cannot fit the visit within a day of waiting
    /// is not reachable; null means no order can visit every stop. <paramref name="hours"/> and <paramref name="stayMinutes"/>
    /// have one entry per stop, in the same order as the matrix's columns.
    /// </summary>
    public static IReadOnlyList<int>? Order(double[,] travel, DateTimeOffset departAt,
        IReadOnlyList<OpeningHours> hours, IReadOnlyList<int> stayMinutes)
    {
        int n = travel.GetLength(0) - 1;
        if (n <= 0) return [];
        if (n > MaxStops) throw new ArgumentException($"At most {MaxStops} stops.", nameof(travel));
        if (hours.Count != n || stayMinutes.Count != n) throw new ArgumentException("One schedule and stay per stop is required.");

        int full = 1 << n;
        var best = new double[full, n];       // seconds after departAt when the visit at the last stop ends
        var previous = new int[full, n];
        for (int mask = 0; mask < full; mask++)
            for (int j = 0; j < n; j++) best[mask, j] = double.PositiveInfinity;

        for (int j = 0; j < n; j++)
        {
            double travelSeconds = travel[0, j + 1];
            if (double.IsPositiveInfinity(travelSeconds)) continue;
            if (VisitEnds(j, travelSeconds, departAt, hours, stayMinutes, out double leave)) best[1 << j, j] = leave;
        }

        for (int mask = 1; mask < full; mask++)
        {
            for (int last = 0; last < n; last++)
            {
                double here = best[mask, last];
                if ((mask & (1 << last)) == 0 || double.IsPositiveInfinity(here)) continue;
                for (int next = 0; next < n; next++)
                {
                    if ((mask & (1 << next)) != 0) continue;
                    double travelSeconds = travel[last + 1, next + 1];
                    if (double.IsPositiveInfinity(travelSeconds)) continue;
                    if (!VisitEnds(next, here + travelSeconds, departAt, hours, stayMinutes, out double leave)) continue;
                    int target = mask | (1 << next);
                    if (leave >= best[target, next]) continue;
                    best[target, next] = leave;
                    previous[target, next] = last;
                }
            }
        }

        int finish = -1;
        double bestEnd = double.PositiveInfinity;
        for (int j = 0; j < n; j++)
        {
            if (best[full - 1, j] >= bestEnd) continue;
            bestEnd = best[full - 1, j];
            finish = j;
        }
        if (finish < 0) return null;

        var order = new int[n];
        int remaining = full - 1;
        for (int position = n - 1; position >= 0; position--)
        {
            order[position] = finish + 1;
            int before = previous[remaining, finish];
            remaining &= ~(1 << finish);
            finish = before;
        }
        return order;
    }

    private static bool VisitEnds(int stop, double arrivalSeconds, DateTimeOffset departAt,
        IReadOnlyList<OpeningHours> hours, IReadOnlyList<int> stayMinutes, out double leaveSeconds)
    {
        leaveSeconds = arrivalSeconds + Math.Max(0, stayMinutes[stop]) * 60.0;
        if (!hours[stop].TryVisit(departAt.AddSeconds(arrivalSeconds), stayMinutes[stop], out DateTimeOffset leave)) return false;
        leaveSeconds = (leave - departAt).TotalSeconds;
        return true;
    }
}
