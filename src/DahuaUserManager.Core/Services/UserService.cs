using DahuaUserManager.Api.Clients;
using DahuaUserManager.Models.Entities;

namespace DahuaUserManager.Core.Services;

public class UserService
{
    private readonly RecordFinderClient _recordFinder = new();
    private readonly DahuaClient _client = new();
    private readonly AccessUserRpcClient _rpcUser = new();
    private readonly FaceUploadClient _faceUpload = new();

    public async Task<bool> CreateUserAsync(
        string ipAddress,
        string username,
        string password,
        AccessUser user)
    {
        return await _rpcUser.CreateUserAsync(
            ipAddress,
            username,
            password,
            user.UserId,
            user.FullName,
            user.ValidFrom ?? DateTime.Today,
            user.ValidTo ?? DateTime.Today.AddYears(10));
    }

    public async Task<bool> UploadUserPhotoAsync(
        string ipAddress,
        string username,
        string password,
        string userId,
        string photoPath,
        int departId)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
            return false;

        return await _faceUpload.UploadFaceAsync(
            ipAddress,
            username,
            password,
            userId,
            photoPath,
            departId);
    }

    public Task<bool> DeleteFaceByUserIdAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        return Task.FromResult(true);
    }

    public async Task<bool> DeleteUserCompletelyAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        await DeleteFaceByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        bool deleted = await _recordFinder.DeleteCardByUserIdAsync(
            ipAddress,
            username,
            password,
            userId);

        return deleted;
    }
}