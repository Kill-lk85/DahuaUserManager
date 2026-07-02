namespace DahuaUserManager.Api.Clients;

public class RecordFinderClient
{
    private readonly DahuaClient _client = new();
    private readonly RecordFinderParser _parser = new();

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

    public async Task<AccessControlCard?> FindCardByUserIdAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        List<AccessControlCard> cards = await GetAccessControlCardsAsync(
            ipAddress,
            username,
            password);

        return cards.FirstOrDefault(x =>
            x.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> DeleteCardByUserIdAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        AccessControlCard? card = await FindCardByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        if (card == null)
            return false;

        string path =
            $"/cgi-bin/recordUpdater.cgi?action=remove&name=AccessControlCard&recno={card.RecNo}";

        await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            path);

        AccessControlCard? check = await FindCardByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        return check == null;
    }
}