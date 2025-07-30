using Contracts.Salesforce.InternalAccountChanged.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceAccountChangedPublisher.Infrastructure.Mapper
{
    public class SalesforceCdcAccountChangedDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string ReplayId { get; set; } = string.Empty;

        // Optional business fields (may be partially populated depending on changed fields)
        public BillingAddressDto? BillingAddress { get; set; }
        public DeliveryAddressDto? DeliveryAddress { get; set; }
        public BillingInfoDto? BillingInfo { get; set; }
        public UserDto? PrimaryContact { get; set; }

        public string EventType { get; set; } = string.Empty;
        public DateTime LastModifiedDate { get; set; }
        public List<string> ChangedFields { get; set; } = new();

        public string Source { get; set; } = "Salesforce";
        public string Publisher { get; set; } = string.Empty;
    }
}
