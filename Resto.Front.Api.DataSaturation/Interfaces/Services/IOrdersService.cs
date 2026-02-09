using System;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface IOrdersService : IDisposable
    {
        void UpdateByCheckBox(bool enableOrdersService);
        void UpdateDataQR(string dataQR);
    }
}
