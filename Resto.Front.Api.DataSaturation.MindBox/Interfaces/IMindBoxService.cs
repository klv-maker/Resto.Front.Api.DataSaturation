using Resto.Front.Api.DataSaturation.MindBox.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.MindBox.Interfaces
{
    public interface IMindBoxService : IDisposable
    {
        Task<CustomerInfo> CheckCustomer(string pointOfContact, string mobilePhone, CancellationToken cancellationToken);
    }
}
