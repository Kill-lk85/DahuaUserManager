using System.Windows.Controls;

namespace DahuaUserManager.UI.Controls;

public partial class ControllerCard : UserControl
{
    public ControllerCard()
    {
        InitializeComponent();
    }

    public void SetData(
        string name,
        string ip,
        string model,
        bool online)
    {
        ControllerName.Text = name;
        ControllerIp.Text = ip;
        ControllerModel.Text = model;

        StatusIcon.Text = online ? "🟢" : "🔴";
    }
}