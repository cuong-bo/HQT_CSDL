using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Hue_travel.Services.Online
{
    /// <summary>
    /// Checks the ECDSA P-256 signature the server puts on every /v1 reply (headers X-Sig-Time and X-Sig), so a person
    /// in the middle of the plain-HTTP connection cannot change what the app shows. The signed text is
    /// "time LF method LF path LF" followed by the exact body bytes. Created from the server's public key (SPKI, base64).
    /// </summary>
    public sealed class ResponseVerifier
    {
        private const int Spki256Length = 91;          // the fixed-size SubjectPublicKeyInfo of a P-256 key
        private const int PointOffset = 26;            // 0x04 || X (32) || Y (32) starts here
        private const int EccPublicP256Magic = 0x31534345;   // "ECS1": BCRYPT_ECDSA_PUBLIC_P256_MAGIC
        private static readonly TimeSpan AllowedClockDifference = TimeSpan.FromHours(1);

        private readonly CngKey key;

        public ResponseVerifier(string spkiBase64)
        {
            byte[] spki = Convert.FromBase64String(spkiBase64.Trim());
            if (spki.Length != Spki256Length || spki[PointOffset] != 0x04) throw new FormatException("The public key is not an ECDSA P-256 SubjectPublicKeyInfo.");

            var blob = new byte[8 + 64];
            BitConverter.GetBytes(EccPublicP256Magic).CopyTo(blob, 0);
            BitConverter.GetBytes(32).CopyTo(blob, 4);
            Array.Copy(spki, PointOffset + 1, blob, 8, 64);
            key = CngKey.Import(blob, CngKeyBlobFormat.EccPublicBlob);
        }

        /// <summary>A verifier for the key, or null when no key is configured or the text is not a usable key.</summary>
        public static ResponseVerifier TryCreate(string spkiBase64)
        {
            if (string.IsNullOrWhiteSpace(spkiBase64)) return null;
            try
            {
                return new ResponseVerifier(spkiBase64);
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
            {
                Debug.WriteLine("ResponseVerifier: " + ex.Message);
                return null;
            }
        }

        /// <summary>True when the reply is genuine. Otherwise <paramref name="problem"/> says why, in words fit for the user.</summary>
        public bool Verify(string method, string path, string time, string signatureBase64, byte[] body, DateTimeOffset now, out string problem)
        {
            problem = null;
            long seconds;
            if (string.IsNullOrEmpty(time) || string.IsNullOrEmpty(signatureBase64) || !long.TryParse(time, out seconds))
            {
                problem = "Phản hồi của máy chủ không có chữ ký hợp lệ. Có thể đường truyền bị can thiệp.";
                return false;
            }

            byte[] signature;
            try
            {
                signature = Convert.FromBase64String(signatureBase64);
            }
            catch (FormatException)
            {
                problem = "Chữ ký của phản hồi không đọc được. Có thể đường truyền bị can thiệp.";
                return false;
            }

            byte[] head = Encoding.UTF8.GetBytes(time + "\n" + method + "\n" + path + "\n");
            var payload = new byte[head.Length + body.Length];
            head.CopyTo(payload, 0);
            body.CopyTo(payload, head.Length);

            bool genuine;
            using (var verifier = new ECDsaCng(key))
            {
                verifier.HashAlgorithm = CngAlgorithm.Sha256;
                genuine = signature.Length == 64 && verifier.VerifyData(payload, signature);
            }
            if (!genuine)
            {
                problem = "Phản hồi của máy chủ không đúng chữ ký. Dữ liệu có thể đã bị sửa trên đường truyền.";
                return false;
            }

            TimeSpan age = now - DateTimeOffset.FromUnixTimeSeconds(seconds);
            if (age > AllowedClockDifference || age < -AllowedClockDifference)
            {
                problem = "Phản hồi quá cũ hoặc đồng hồ máy tính lệch giờ. Hãy kiểm tra ngày giờ của máy.";
                return false;
            }
            return true;
        }

        /// <summary>The path as the server sees it (decoded, no query string).</summary>
        public static string SignedPath(Uri uri) => Uri.UnescapeDataString(uri.AbsolutePath);
    }
}
