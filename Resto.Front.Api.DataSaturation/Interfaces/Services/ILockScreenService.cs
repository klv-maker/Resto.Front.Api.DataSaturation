using System;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface ILockService : IDisposable
    {
        void UpdateSwitchMediaTime(int newSwitchMediaTime);
    }
}
