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
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, 80);

            using NetworkStream stream = client.GetStream();

            var requestBuilder = new StringBuilder();

            requestBuilder.Append($"GET {path} HTTP/1.1\r\n");
            requestBuilder.Append($"Host: {ipAddress}\r\n");
            requestBuilder.Append("User-Agent: DahuaUserManager/1.0\r\n");
            requestBuilder.Append("Accept: */*\r\n");
            requestBuilder.Append("Connection: close\r\n");

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    requestBuilder.Append($"{header.Key}: {header.Value}\r\n");
                }
            }

            requestBuilder.Append("\r\n");

            byte[] requestBytes = Encoding.ASCII.GetBytes(requestBuilder.ToString());

            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
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