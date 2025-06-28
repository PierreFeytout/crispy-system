using System.Security.Cryptography;
using System.Text;

// CHANGE THIS to your own (less obvious) marker
const string markerStr = "\0\0v0lt_mAGic4ppEND\0\0";

if (args.Length != 1)
{
    Console.WriteLine("Usage: AppendEncryptedApiKey.exe <exePath>");
    return;
}

string exePath = args[0];
string apiKey = "nL2mU9jx6_-r_2Zy0wQaC9Fz2uAxPyUKbRt3Wz0B7QrOgxNRzINXIsQEGTKaI3pa7GLQ4N34hBrQAnrfUJ_mfQ";
string passphrase = "Timer500Cron";

// Generate AES IV
using var aes = Aes.Create();
aes.GenerateIV();
byte[] iv = aes.IV;

// Derive key from passphrase
byte[] key = new Rfc2898DeriveBytes(passphrase, iv, 10000).GetBytes(32); // 256-bit

byte[] encrypted = EncryptStringToBytes_Aes(apiKey, key, iv);

using var fs = new FileStream(exePath, FileMode.Append, FileAccess.Write);
using var bw = new BinaryWriter(fs);

// Write marker
bw.Write(Encoding.UTF8.GetBytes(markerStr));

// Write IV length and IV
bw.Write(BitConverter.GetBytes(iv.Length));
bw.Write(iv);

// Write encrypted API key length and bytes
bw.Write(BitConverter.GetBytes(encrypted.Length));
bw.Write(encrypted);

Console.WriteLine("Appended encrypted API key to: " + exePath);

// --- AES Encryption Helper ---
static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
{
    using var aesAlg = Aes.Create();
    aesAlg.Key = Key;
    aesAlg.IV = IV;
    using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
    using var msEncrypt = new MemoryStream();
    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
    using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
    {
        swEncrypt.Write(plainText);
    }
    return msEncrypt.ToArray();
}
