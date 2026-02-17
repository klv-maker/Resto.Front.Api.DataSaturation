using Resto.Front.Api.DataSaturation.Settings;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettings
    {
        List<string> AdressesApi { get; }
        int SwitchMediaTime { get; }
        bool EnableOrdersService { get; }
        string DataQR { get; }
        IikoCard IikoCard { get; }
        void Update(List<string> addresses, int switchMediaTime, bool enableOrdersService, string dataQR, IikoCard iikoCard);
    }
}
