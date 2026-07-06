using System.Net.Sockets;
using System.Text;

namespace DahuaUserManager.Api.Clients;

public class RawHttpClient
{
    public async Task<string> SendGetAsync(
        string ipAddress,
        string path,
        Dictionary<string, string>? headers = null)
    {
        return await SendAsync(
            ipAddress,
            "GET",
            path,
            "",
            "text/plain; charset=utf-8",
            headers);
    }

    public async Task<string> SendPostAsync(
        string ipAddress,
        string path,
        string body,
        string contentType = "application/x-www-form-urlencoded",
        Dictionary<string, string>? headers = null)
    {
        return await SendAsync(
            ipAddress,
            "POST",
            path,
            body,
            contentType,
            headers);
    }

    private static async Task<string> SendAsync(
        string ipAddress,
        string method,
        string path,
        string body,
        string contentType,
        Dictionary<string, string>? headers)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, 80);

            using NetworkStream stream = client.GetStream();

            byte[] bodyBytes = Encoding.UTF8.GetBytes(body ?? "");

            var requestBuilder = new StringBuilder();

            requestBuilder.Append($"{method} {path} HTTP/1.1\r\n");
            requestBuilder.Append($"Host: {ipAddress}\r\n");
            requestBuilder.Append("User-Agent: DahuaUserManager/1.0\r\n");
            requestBuilder.Append("Accept: */*\r\n");
            requestBuilder.Append("Connection: close\r\n");

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                requestBuilder.Append($"Content-Type: {contentType}\r\n");
                requestBuilder.Append($"Content-Length: {bodyBytes.Length}\r\n");
            }

            if (headers != null)
            {
                foreach (var header in headers)
                    requestBuilder.Append($"{header.Key}: {header.Value}\r\n");
            }

            requestBuilder.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(requestBuilder.ToString());

            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

            if (bodyBytes.Length > 0)
                await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);

            await stream.FlushAsync();

            using var memory = new MemoryStream();
            var buffer = new byte[4096];

            while (true)
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (read <= 0)
                    break;

                memory.Write(buffer, 0, read);
            }

            byte[] responseBytes = memory.ToArray();

            if (responseBytes.Length == 0)
                return "EMPTY RESPONSE";

            return Encoding.UTF8.GetString(responseBytes);
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}