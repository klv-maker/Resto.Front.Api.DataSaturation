using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Domain.Entities
{
    public class OrganizationGuestInfo
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string cultureName { get; set; }
        public DateTime? birthday { get; set; }
        public string email { get; set; }
        public string surname { get; set; }
        public Sex sex { get; set; }
        public List<CustomerPhone> additionalPhones { get; set; } = new List<CustomerPhone>();
        public List<GuestCardInfo> cards { get; set; } = new List<GuestCardInfo>();
        public List<GuestCategoryInfo> categories { get; set; } = new List<GuestCategoryInfo>();
        public List<UserWalletInfo> walletBalances { get; set; } = new List<UserWalletInfo>();
        public string middleName { get; set; }
        public Guid? referrerId { get; set; }
        public string userData { get; set; }
    }

    public class UserWalletInfo
    {
        public WalletInfo wallet { get; set; }
        public decimal balance { get; set; }
    }

    public class WalletInfo
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public WalletTypes type { get; set; }
        public ProgramType programType { get; set; }
    }

    public enum ProgramType
    {
        Money,
        Bonus,
        Product,
    }

    public enum WalletTypes 
    {
        Bonus,
        IikoCard,
        IikoCardInteger,
        Real
    }

    public class GuestCategoryInfo
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public bool isDefaultForNewGuests { get; set; }
    }

    public class GuestCardInfo 
    {
        public Guid Id { get; set; }
        public string Track { get; set; }
        public string Number { get; set; }
        public bool IsActivated { get; set; }

    }

    public class CustomerPhone
    {
        public string phone { get; set; }
    }

    public enum Sex
    {
        None,
        Male,
        Female
    }
}
