using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettings
    {
        List<string> AdressesApi { get; set; }
        int SwitchMediaTime { get; set; }
        void Update(List<string> addresses, int switchMediaTime);
    }
}
