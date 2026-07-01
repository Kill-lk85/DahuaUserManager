namespace DahuaUserManager.Api.Clients;

public class DahuaClient
{
    private readonly RawHttpClient _rawClient = new();
    private readonly DigestAuthenticator _digest = new();

    public async Task<SystemInfo> GetSystemInfoAsync(
        string ipAddress,
        string username,
        string password)
    {
        string body = await ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            "/cgi-bin/magicBox.cgi?action=getSystemInfo");

        return ParseSystemInfo(body);
    }

    public async Task<string> ExecuteAuthenticatedGetAsync(
        string ipAddress,
        string username,
        string password,
        string path)
    {
        string firstResponse = await _rawClient.SendGetAsync(ipAddress, path);

        string authLine = firstResponse
            .Replace("\r\n", "\n")
            .Split('\n')
            .FirstOrDefault(x => x.TrimStart().StartsWith("WWW-Authenticate:", StringComparison.OrdinalIgnoreCase))
            ?? "";

        if (string.IsNullOrWhiteSpace(authLine))
            throw new Exception("WWW-Authenticate не найден.\n\n" + firstResponse);

        string digestHeader = authLine
            .Substring(authLine.IndexOf(':') + 1)
            .Trim();

        DigestInfo digestInfo = _digest.Parse(digestHeader);

        string authorization = _digest.CreateAuthorizationHeader(
            username,
            password,
            "GET",
            path,
            digestInfo);

        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = authorization
        };

        string secondResponse = await _rawClient.SendGetAsync(
            ipAddress,
            path,
            headers);

        if (!secondResponse.StartsWith("HTTP/1.1 200"))
            throw new Exception("Ошибка авторизованного запроса:\n\n" + secondResponse);

        return ExtractBody(secondResponse);
    }

    private static string ExtractBody(string response)
    {
        int index = response.IndexOf("\r\n\r\n", StringComparison.Ordinal);

        if (index < 0)
            return "";

        return response[(index + 4)..];
    }

    private static SystemInfo ParseSystemInfo(string body)
    {
        var info = new SystemInfo();

        foreach (string line in body.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split('=', 2);

            if (parts.Length != 2)
                continue;

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            switch (key)
            {
                case "deviceType":
                    info.DeviceType = value;
                    break;

                case "hardwareVersion":
                    info.HardwareVersion = value;
                    break;

                case "processor":
                    info.Processor = value;
                    break;

                case "serialNumber":
                    info.SerialNumber = value;
                    break;

                case "updateSerial":
                    info.UpdateSerial = value;
                    break;
            }
        }

        return info;
    }
}