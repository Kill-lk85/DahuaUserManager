namespace DahuaUserManager.Api.Clients;

public class RecordFinderClient
{
    private readonly DahuaClient _client = new();
    private readonly RecordFinderParser _parser = new();

    /// <summary>
    /// Получить список пользователей/карт контроллера.
    /// </summary>
    public async Task<List<AccessControlCard>> GetAccessControlCardsAsync(
        string ipAddress,
        string username,
        string password)
    {
        const string path =
            "/cgi-bin/recordFinder.cgi?action=find&name=AccessControlCard";

        string response = await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            path);

        return _parser.ParseCards(response);
    }

    /// <summary>
    /// Найти пользователя по UserID.
    /// </summary>
    public async Task<AccessControlCard?> FindCardByUserIdAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        List<AccessControlCard> cards =
            await GetAccessControlCardsAsync(
                ipAddress,
                username,
                password);

        return cards.FirstOrDefault(x =>
            x.UserId.Equals(
                userId,
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Удалить пользователя по UserID.
    /// </summary>
    public async Task<bool> DeleteCardByUserIdAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        AccessControlCard? card =
            await FindCardByUserIdAsync(
                ipAddress,
                username,
                password,
                userId);

        if (card == null)
            return false;

        string path =
            $"/cgi-bin/recordUpdater.cgi?action=remove" +
            $"&name=AccessControlCard" +
            $"&recno={card.RecNo}";

        await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            path);

        AccessControlCard? check =
            await FindCardByUserIdAsync(
                ipAddress,
                username,
                password,
                userId);

        return check == null;
    }

    /// <summary>
    /// Получить весь доступный журнал событий.
    /// </summary>
    public async Task<string> GetAccessControlRecordRawAsync(
        string ipAddress,
        string username,
        string password)
    {
        const string path =
            "/cgi-bin/recordFinder.cgi" +
            "?action=find" +
            "&name=AccessControlCardRec" +
            "&count=1024";

        return await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            path);
    }

    /// <summary>
    /// Получить журнал событий за указанный период.
    /// StartTime и EndTime передаются контроллеру
    /// как Unix timestamp в секундах.
    /// </summary>
    public async Task<string> GetAccessControlRecordRawAsync(
        string ipAddress,
        string username,
        string password,
        DateTime startTime,
        DateTime endTime,
        int count = 1024)
    {
        long startUnix = new DateTimeOffset(
            startTime).ToUnixTimeSeconds();

        long endUnix = new DateTimeOffset(
            endTime).ToUnixTimeSeconds();

        string path =
            "/cgi-bin/recordFinder.cgi" +
            "?action=find" +
            "&name=AccessControlCardRec" +
            $"&StartTime={startUnix}" +
            $"&EndTime={endUnix}" +
            $"&count={count}";

        return await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            path);
    }
}