using DahuaUserManager.Models.Entities;
using DahuaUserManager.Core.Services;

namespace DahuaUserManager.Core.Managers;

public class ControllerManager
{
    private readonly ConfigurationService _configurationService = new();

    private AppSettings _settings = new();

    public List<ControllerInfo> Controllers => _settings.Controllers;

    public void Load()
    {
        _settings = _configurationService.Load();
    }

    public void Save()
    {
        _configurationService.Save(_settings);
    }

    public bool AddController(string ipAddress)
    {
        ipAddress = ipAddress.Trim();

        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (_settings.Controllers.Any(x => x.IpAddress == ipAddress))
            return false;

        _settings.Controllers.Add(new ControllerInfo
        {
            Name = ipAddress,
            IpAddress = ipAddress,
            Username = "admin",
            Password = "Admin123!"
        });

        Save();

        return true;
    }

    public bool RemoveController(string ipAddress)
    {
        ControllerInfo? controller =
            _settings.Controllers.FirstOrDefault(x => x.IpAddress == ipAddress);

        if (controller == null)
            return false;

        _settings.Controllers.Remove(controller);

        Save();

        return true;
    }
}