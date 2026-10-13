using Hue_travel.Api.Auth;
using Hue_travel.Api.Routing;
using Hue_travel.Api.Weather;

namespace Hue_travel.Api.Tests;

public class RouteOptimizerTests
{
    private static double[,] Matrix(double[][] rows)
    {
        var m = new double[rows.Length, rows.Length];
        for (int i = 0; i < rows.Length; i++)
            for (int j = 0; j < rows.Length; j++) m[i, j] = rows[i][j];
        return m;
    }

    [Fact]
    public void Order_ReturnsNothing_WhenThereAreNoStops() =>
        Assert.Empty(RouteOptimizer.Order(new double[1, 1])!);

    [Fact]
    public void Order_PicksTheCheapestOpenPath()
    {
        // start(0) - a(1) - b(2) - c(3) lie on a line: the best order is a, b, c.
        double[,] cost = Matrix(
        [
            [0, 1, 2, 3],
            [1, 0, 1, 2],
            [2, 1, 0, 1],
            [3, 2, 1, 0],
        ]);
        Assert.Equal([1, 2, 3], RouteOptimizer.Order(cost)!);
    }

    [Fact]
    public void Order_UsesDirectionalCosts()
    {
        // Going 1 -> 2 is cheap, 2 -> 1 is very expensive: the order must be 1, 2.
        double[,] cost = Matrix(
        [
            [0, 5, 5],
            [5, 0, 1],
            [5, 100, 0],
        ]);
        Assert.Equal([1, 2], RouteOptimizer.Order(cost)!);
    }

    [Fact]
    public void Order_ReturnsNull_WhenAStopCannotBeReached()
    {
        double inf = double.PositiveInfinity;
        double[,] cost = Matrix([[0, 1, inf], [1, 0, inf], [inf, inf, 0]]);
        Assert.Null(RouteOptimizer.Order(cost));
    }

    [Fact]
    public void Order_RejectsTooManyStops() =>
        Assert.Throws<ArgumentException>(() => RouteOptimizer.Order(new double[RouteOptimizer.MaxStops + 2, RouteOptimizer.MaxStops + 2]));
}

public class AuthValidationTests
{
    [Theory]
    [InlineData("an", "matkhau123")]
    [InlineData("has space", "matkhau123")]
    [InlineData("nguoidung", "ngan")]
    public void ValidateRegistration_RejectsBadInput(string username, string password) =>
        Assert.NotNull(AuthValidation.ValidateRegistration(username, password, null));

    [Fact]
    public void ValidateRegistration_RejectsPasswordsOver72Bytes() =>
        Assert.NotNull(AuthValidation.ValidateRegistration("nguoidung", new string('a', 73), null));

    [Fact]
    public void ValidateRegistration_AcceptsGoodInput() =>
        Assert.Null(AuthValidation.ValidateRegistration("nguoi.dung_1", "matkhau123", "Nguyễn Văn A"));
}

public class PasswordAndTokenTests
{
    [Fact]
    public void PasswordHasher_VerifiesOnlyTheRightPassword()
    {
        string hash = PasswordHasher.Hash("matkhau123");
        Assert.True(PasswordHasher.Verify("matkhau123", hash));
        Assert.False(PasswordHasher.Verify("matkhau124", hash));
    }

    [Fact]
    public void RefreshTokens_AreRandomAndHashedConsistently()
    {
        (string a, byte[] hashA) = TokenService.NewRefreshToken();
        (string b, _) = TokenService.NewRefreshToken();
        Assert.NotEqual(a, b);
        Assert.Equal(hashA, TokenService.HashRefreshToken(a));
    }
}

public class WeatherParserTests
{
    [Fact]
    public void TryParse_ReadsTheCurrentBlock()
    {
        const string json = "{\"current\":{\"temperature_2m\":24.3,\"apparent_temperature\":29.1,\"weather_code\":61,\"is_day\":0,\"precipitation\":0.4,\"wind_speed_10m\":12.5,\"uv_index\":0}}";
        WeatherDto? w = WeatherParser.TryParse(json, DateTimeOffset.UnixEpoch);
        Assert.NotNull(w);
        Assert.Equal(24.3, w!.Temperature);
        Assert.Equal(29.1, w.ApparentTemperature);
        Assert.Equal(61, w.Code);
        Assert.False(w.IsDay);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"current\":{}}")]
    public void TryParse_ReturnsNull_ForBadResponses(string json) =>
        Assert.Null(WeatherParser.TryParse(json, DateTimeOffset.UnixEpoch));
}
