using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettings
    {
        List<string> AdressesApi { get; }
        int SwitchMediaTime { get; }
        void Update(List<string> addresses, int switchMediaTime);
    }
}
