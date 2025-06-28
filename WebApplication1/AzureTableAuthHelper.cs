using System.Security.Cryptography;

public static class AzureTableAuthHelper
{
    public static string BuildAuthHeader(
        string accountName,
        string accountKey,
        string httpMethod,
        string canonicalizedResource,
        string contentMd5 = "",
        string contentType = "",
        string date = null)
    {
        date ??= DateTime.UtcNow.ToString("R");

        // Build the string-to-sign (Table Storage style)
        var stringToSign = $"{httpMethod}\n{contentMd5}\n{contentType}\n{date}\n{canonicalizedResource}";

        var keyBytes = Convert.FromBase64String(accountKey);
        using var hmac = new HMACSHA256(keyBytes);
        var signatureBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stringToSign));
        var signature = Convert.ToBase64String(signatureBytes);

        Console.WriteLine("StringToSign:");
        Console.WriteLine(stringToSign.Replace("\n", "\\n\n"));
        return $"SharedKey {accountName}:{signature}";
    }

    // Canonicalized resource must not include () or extra slashes
    public static string GetCanonicalizedResource(string accountName, string resourcePath)
        => $"/{accountName}/{resourcePath}";
}
