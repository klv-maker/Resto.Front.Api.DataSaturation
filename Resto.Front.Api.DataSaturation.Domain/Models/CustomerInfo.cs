using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Domain.Models
{
    public class CustomerInfo
    {
        public CustomerData userData { get; set; }
        public List<CustomerWallet> userWallets { get; set; } = new List<CustomerWallet>();
    }

    public class CustomerWallet
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public bool isInteger { get; set; }
        public decimal balance { get; set; }
    }

    public class CustomerData
    {
        public string name { get; set; }
        public string lastName { get; set; }
        public string phone { get; set; }
        public DateTime? birthday { get; set; } = null;
        public Sex sex { get; set; }
    }


    public class CustomerAddData
    {
        public string cardNumber { get; set; }
        public string cardTrack { get; set; }
        public CustomerAddDataInfo userData { get; set; }
    }

    public class CustomerAddDataInfo : CustomerData
    {
        public string fullName { get; set; }
        public Guid? referrer { get; set; }
    }

}
