using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface IIikoCardService : IDisposable
    {
        void UpdateSettings(IikoCard iikoCard);
        Task<OrganizationGuestInfo> GetCustomerAsync(string customerId, CancellationToken cancellationToken);
    }
}
