using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettings
    {
        List<string> AdressesApi { get; set; }
        void Update(List<string> addresses);
    }
}
