using DahuaUserManager.Api.Clients;
using DahuaUserManager.Core.Mappers;
using DahuaUserManager.Models.Entities;

namespace DahuaUserManager.Core.Services;

public class UserService
{
    private readonly RecordFinderClient _recordFinder = new();
    private readonly DahuaClient _client = new();

    public async Task<bool> CreateUserAsync(
        string ipAddress,
        string username,
        string password,
        AccessUser user)
    {
        AccessControlCard card = UserMapper.ToAccessControlCard(user);

        string path =
            "/cgi-bin/recordUpdater.cgi?action=insert&name=AccessControlCard" +
            $"&CardName={Uri.EscapeDataString(card.CardName)}" +
            $"&CardNo={Uri.EscapeDataString(card.CardNo)}" +
            $"&CardStatus={card.CardStatus}" +
            $"&IsValid={card.IsValid.ToString().ToLower()}" +
            $"&UserID={Uri.EscapeDataString(card.UserId)}" +
            $"&ValidDateStart={Uri.EscapeDataString(card.ValidDateStart)}" +
            $"&ValidDateEnd={Uri.EscapeDataString(card.ValidDateEnd)}";

        string response = await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            path);

        return response.Trim().Contains("OK", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> DeleteFaceByUserIdAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        string response = await _client.ExecuteAuthenticatedGetAsync(
            ipAddress,
            username,
            password,
            $"/cgi-bin/FaceInfoManager.cgi?action=remove&UserID={userId}");

        return response.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> DeleteUserCompletelyAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        await DeleteFaceByUserIdAsync(ipAddress, username, password, userId);

        bool deleted = await _recordFinder.DeleteCardByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        return deleted;
    }
}