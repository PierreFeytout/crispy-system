using System.Net.Security;
using System.Security.Cryptography;

namespace SpaceInvader;
public static class SocketHandlerBuilder
{
    public static SocketsHttpHandler Build()
    {

        // C'est la clé publique SHA256 en binaire
        byte[] pinnedKey = Convert.FromHexString("3082010A0282010100AEF82029126B48692A0AB4C081271F39F9F779B139AD63B56DFA703D33E1FD33A97791D618E5559397750DD0138234490035672B060F01F30C5C17E635BB7EB23AF7F4D4C66AB36A3F818A442C3719DB84ADB5EB8D93AF31532CABB476BA1C45DA18AC3C2B74724193FB2A61C4C5CE668A835EAA4E6958A585FEC9D4BB7FBCA038B32346EB5278480814D7A4CD31FE6B6E507EC059BD50A151FDA48FF834AAB19DF7FFD009A936D5CC1124CAFF0BF4EB9B25A2A16EDF60EA6CA2B44EA8E742D9139FD7B35FAD8A03C844609120F97D9DB9AC4AF487F5E7073018C9462F0F1381843126D029E423792E0BB0C977A99620BCA3CBBEF58CB3A744DFBB1E2ABC7CD1020301000A");

        var handler = new SocketsHttpHandler();
        handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
        {
            // Si le handshake SSL a échoué, rejet direct
            if (errors != SslPolicyErrors.None)
                return false;

            // Extraire la clé publique du certificat serveur
            using var sha256 = SHA256.Create();
            byte[] publicKey = cert.GetPublicKey();

            var pinnedHash = sha256.ComputeHash(pinnedKey);

            // Calculer le hash SHA-256 de la clé publique
            byte[] publicKeyHash = sha256.ComputeHash(publicKey);

            // Comparer avec la valeur attendue
            return publicKeyHash.SequenceEqual(pinnedHash);
        };

        return handler;

    }
}