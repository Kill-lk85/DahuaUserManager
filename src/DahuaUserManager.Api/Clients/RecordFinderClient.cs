namespace DahuaUserManager.Api.Clients;

public class RecordFinderClient
{
    private readonly DahuaClient _client = new();
    private readonly RecordFinderParser _parser = new();

    public async Task<List<AccessControlCard>> GetAccessControlCardsAsync(
        ControllerInfo controller)
    {
        string response = await _client.ExecuteAuthenticatedGetAsync(
            controller.IpAddress,
            controller.Username,
            controller.Password,
            "/cgi-bin/recordFinder.cgi?action=find&name=AccessControlCard");

        return _parser.ParseCards(response);
    }

    public async Task<AccessControlCard?> FindCardByUserIdAsync(
        ControllerInfo controller,
        string userId)
    {
        List<AccessControlCard> cards =
            await GetAccessControlCardsAsync(controller);

        return cards.FirstOrDefault(c =>
            c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> DeleteCardByUserIdAsync(
        ControllerInfo controller,
        string userId)
    {
        AccessControlCard? card =
            await FindCardByUserIdAsync(controller, userId);

        if (card == null)
            return false;

        string path =
            $"/cgi-bin/recordUpdater.cgi?action=remove&name=AccessControlCard&recno={card.RecNo}";

        await _client.ExecuteAuthenticatedGetAsync(
            controller.IpAddress,
            controller.Username,
            controller.Password,
            path);

        return await FindCardByUserIdAsync(controller, userId) == null;
    }
}