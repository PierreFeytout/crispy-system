using System.Security.Cryptography;
using System.Text;

public static class ApiKeyLoader
{

    private const string markerStr = "\0\0v0lt_mAGic4ppEND\0\0";
    // Adjust marker as needed. Must match what you use during encryption/append.
    private static readonly byte[] Marker = Encoding.UTF8.GetBytes(markerStr);

    /// <summary>
    /// Loads and decrypts the API key appended at the end of the current executable.
    /// </summary>
    /// <param name="passphrase">The passphrase used for AES key derivation.</param>
    /// <returns>The decrypted API key as a string.</returns>
    public static string LoadDecryptedApiKey(string passphrase)
    {
        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            // Seek from the end and find the marker
            long markerPos = FindMarkerPosition(fs, Marker);
            if (markerPos < 0)
                throw new InvalidOperationException("API key marker not found in executable.");

            fs.Seek(markerPos + Marker.Length, SeekOrigin.Begin);

            // Read IV length (int32, big endian)
            byte[] ivLenBytes = new byte[4];
            fs.Read(ivLenBytes, 0, 4);
            int ivLength = BitConverter.ToInt32(ivLenBytes, 0);

            // Read IV
            byte[] iv = new byte[ivLength];
            fs.Read(iv, 0, ivLength);

            // Read encrypted API key length (int32, big endian)
            byte[] encLenBytes = new byte[4];
            fs.Read(encLenBytes, 0, 4);
            int encLength = BitConverter.ToInt32(encLenBytes, 0);

            // Read encrypted API key
            byte[] encrypted = new byte[encLength];
            fs.Read(encrypted, 0, encLength);

            // Derive key from passphrase (e.g., using SHA256)
            byte[] key = new Rfc2898DeriveBytes(passphrase, iv, 10000).GetBytes(32); // 256-bit key

            return DecryptStringFromBytes_Aes(encrypted, key, iv);
        }
    }

    // Finds the marker in the file and returns its position
    private static long FindMarkerPosition(FileStream fs, byte[] marker)
    {
        long originalPos = fs.Position;
        long length = fs.Length;
        int chunkSize = 4096;
        byte[] buffer = new byte[chunkSize + marker.Length];
        long scanStart = Math.Max(0, length - (1024 * 1024)); // Only scan last 1MB

        fs.Seek(scanStart, SeekOrigin.Begin);
        int bytesRead;
        long pos = scanStart;
        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
        {
            int index = IndexOf(buffer, marker, bytesRead);
            if (index >= 0)
                return pos + index;
            pos += bytesRead - marker.Length;
            if (pos + marker.Length >= length)
                break;
            fs.Seek(pos, SeekOrigin.Begin);
        }
        fs.Seek(originalPos, SeekOrigin.Begin);
        return -1;
    }

    // Helper: Find byte pattern
    private static int IndexOf(byte[] buffer, byte[] pattern, int length)
    {
        for (int i = 0; i <= length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (buffer[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }

    // Decrypt AES
    public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;
        using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        using var msDecrypt = new MemoryStream(cipherText);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);
        return srDecrypt.ReadToEnd();
    }
}
