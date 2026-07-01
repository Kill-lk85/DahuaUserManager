using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DahuaUserManager.Api.Clients;

public class DigestAuthenticator
{
    public DigestInfo Parse(string header)
    {
        var info = new DigestInfo
        {
            Realm = GetValue(header, "realm"),
            Nonce = GetValue(header, "nonce"),
            Opaque = GetValue(header, "opaque"),
            Qop = GetValue(header, "qop"),
            Algorithm = GetValue(header, "algorithm")
        };

        if (string.IsNullOrWhiteSpace(info.Qop))
            info.Qop = "auth";

        if (string.IsNullOrWhiteSpace(info.Algorithm))
            info.Algorithm = "MD5";

        return info;
    }

    public string CreateAuthorizationHeader(
        string username,
        string password,
        string method,
        string uri,
        DigestInfo info)
    {
        const string nc = "00000001";
        string cnonce = GenerateCnonce();

        string ha1 = Md5Hex($"{username}:{info.Realm}:{password}");
        string ha2 = Md5Hex($"{method}:{uri}");

        string response = Md5Hex(
            $"{ha1}:{info.Nonce}:{nc}:{cnonce}:{info.Qop}:{ha2}");

        var sb = new StringBuilder();

        sb.Append("Digest ");
        sb.Append($"username=\"{username}\", ");
        sb.Append($"realm=\"{info.Realm}\", ");
        sb.Append($"nonce=\"{info.Nonce}\", ");
        sb.Append($"uri=\"{uri}\", ");
        sb.Append($"algorithm={info.Algorithm}, ");
        sb.Append($"response=\"{response}\", ");
        sb.Append($"qop={info.Qop}, ");
        sb.Append($"nc={nc}, ");
        sb.Append($"cnonce=\"{cnonce}\"");

        if (!string.IsNullOrWhiteSpace(info.Opaque))
        {
            sb.Append($", opaque=\"{info.Opaque}\"");
        }

        return sb.ToString();
    }

    private static string GetValue(string source, string name)
    {
        var match = Regex.Match(
            source,
            $"{name}=\"?([^\",]+)\"?",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : "";
    }

    private static string Md5Hex(string input)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(input);
        byte[] hash = MD5.HashData(bytes);

        var sb = new StringBuilder();

        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    private static string GenerateCnonce()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(8);

        var sb = new StringBuilder();

        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}