using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.Core.Services;

public class UserService
{
    private readonly RecordFinderClient _recordFinder = new();
    private readonly DahuaClient _client = new();

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
        // 1. Удаляем лицо (если его нет — команда всё равно возвращает OK)
        await DeleteFaceByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        // 2. Удаляем карточку пользователя
        bool deleted = await _recordFinder.DeleteCardByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        return deleted;
    }
}