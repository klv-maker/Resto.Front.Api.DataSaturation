using Resto.Front.Api.Data.Orders;
using System;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface IScreensService : IDisposable
    {
        EventHandler<IOrder> OrderScreenOpened { get; set; }
        EventHandler<bool> LockScreenChanged { get; set; }
    }
}
