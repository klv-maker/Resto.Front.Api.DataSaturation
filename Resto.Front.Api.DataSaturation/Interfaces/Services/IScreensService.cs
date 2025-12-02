using Resto.Front.Api.Data.Orders;
using System;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface IScreensService : IDisposable
    {
        EventHandler<IOrder> OrderScreenOpened { get; set; }
        EventHandler<bool> LockScreenChanged { get; set; }
        /// <summary>
        /// для явного вызова при запуске открытия окна с медиа
        /// </summary>
        /// <param name="isOpen"></param>
        void SendLockScreenChanged(bool isOpen);
    }
}
