using Resto.Front.Api.DataSaturation.Settings;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettings
    {
        SettingsListener Listener { get; set; }
    }
}
