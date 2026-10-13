using System.Security.Cryptography;
using System.Text;

namespace Hue_travel.Api.Security;

/// <summary>
/// Signs every /v1 response with ECDSA P-256 so the desktop app can tell that the data was not changed on the way.
/// This is NOT encryption (the traffic is plain HTTP): it protects the integrity of what the app shows, not its secrecy.
/// The signed text is: time, method, decoded path and the exact body bytes, each prefix line ending in LF.
/// </summary>
public sealed class ResponseSigner
{
    public const string TimeHeader = "X-Sig-Time";
    public const string SignatureHeader = "X-Sig";

    private readonly ECDsa? key;

    public ResponseSigner(IConfiguration configuration)
    {
        string? base64 = configuration["Signing:PrivateKeyPkcs8"];
        if (string.IsNullOrWhiteSpace(base64)) return;
        key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(Convert.FromBase64String(base64), out _);
        if (key.KeySize != 256) throw new InvalidOperationException("Signing:PrivateKeyPkcs8 must be an ECDSA P-256 key.");
    }

    public bool Enabled => key != null;

    public static byte[] Payload(string time, string method, string path, ReadOnlySpan<byte> body)
    {
        byte[] head = Encoding.UTF8.GetBytes($"{time}\n{method}\n{path}\n");
        var all = new byte[head.Length + body.Length];
        head.CopyTo(all, 0);
        body.CopyTo(all.AsSpan(head.Length));
        return all;
    }

    /// <summary>The signature as base64 (64 bytes: r then s).</summary>
    public string Sign(string time, string method, string path, ReadOnlySpan<byte> body) =>
        Convert.ToBase64String(key!.SignData(Payload(time, method, path, body), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
}

public static class ResponseSigningExtensions
{
    /// <summary>Buffers each /v1 response, signs it and adds the X-Sig-Time and X-Sig headers. Does nothing when no key is configured.</summary>
    public static IApplicationBuilder UseResponseSigning(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var signer = context.RequestServices.GetRequiredService<ResponseSigner>();
            if (!signer.Enabled || !context.Request.Path.StartsWithSegments("/v1"))
            {
                await next();
                return;
            }

            Stream original = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await next();
            }
            finally
            {
                context.Response.Body = original;
            }

            byte[] body = buffer.ToArray();
            string time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            context.Response.Headers[ResponseSigner.TimeHeader] = time;
            context.Response.Headers[ResponseSigner.SignatureHeader] = signer.Sign(time, context.Request.Method, context.Request.Path.Value ?? "", body);
            context.Response.ContentLength = body.Length;
            await original.WriteAsync(body, context.RequestAborted);
        });
}
