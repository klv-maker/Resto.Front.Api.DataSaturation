using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettings
    {
        List<string> AdressesApi { get; }
        int SwitchMediaTime { get; }
        bool EnableOrdersService { get; }
        void Update(List<string> addresses, int switchMediaTime, bool enableOrdersService);
    }
}
