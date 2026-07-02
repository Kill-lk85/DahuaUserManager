using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DahuaUserManager.UI.ViewModels;

public class ControllerViewModel : INotifyPropertyChanged
{
    private string _name = "";
    private string _ipAddress = "";
    private string _model = "";
    private string _firmware = "";
    private bool _isOnline;
    private int _userCount;
    private int _responseTime;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string IpAddress
    {
        get => _ipAddress;
        set => SetField(ref _ipAddress, value);
    }

    public string Model
    {
        get => _model;
        set => SetField(ref _model, value);
    }

    public string Firmware
    {
        get => _firmware;
        set => SetField(ref _firmware, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set => SetField(ref _isOnline, value);
    }

    public int UserCount
    {
        get => _userCount;
        set => SetField(ref _userCount, value);
    }

    public int ResponseTime
    {
        get => _responseTime;
        set => SetField(ref _responseTime, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}