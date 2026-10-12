namespace Hue_travel.Api.Auth;

public static class PasswordHasher
{
    private const int WorkFactor = 11;

    // Verified against when the user does not exist, so a wrong user name and a wrong password take equally long.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("not-a-real-password", WorkFactor);

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public static bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public static void SpendTimeLikeAVerification() => BCrypt.Net.BCrypt.Verify("x", DummyHash);
}
