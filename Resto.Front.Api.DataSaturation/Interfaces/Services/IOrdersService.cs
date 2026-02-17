using System;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface IOrdersService : IDisposable
    {
        void UpdateSettings(bool enableOrdersService, string dataQR);
    }
}
