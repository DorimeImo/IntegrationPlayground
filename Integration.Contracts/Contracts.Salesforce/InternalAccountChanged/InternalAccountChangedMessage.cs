using Contracts.Salesforce.InternalAccountChanged.DTOs;
using Contracts.Salesforce.InternalAccountChanged.Enums;
using NServiceBus;

namespace Contracts.Salesforce.InternalAccountChanged
{
    //Master: Salesforce
    public class InternalAccountChangedMessage : IEvent
    {
        // Core Identity
        public required Guid AccountId { get; set; } 
        public required string AccountName { get; set; } 
        public required string AccountType { get; set; }
        public required string ReplayId { get; set; }

        // Business Data
        public BillingAddressDto? BillingAddress { get; set; }
        public DeliveryAddressDto? DeliveryAddress { get; set; }
        public BillingInfoDto? BillingInfo { get; set; }
        public UserDto? PrimaryContact { get; set; }

        // Metadata
        public required AccountEventType EventType { get; set; }
        public required List<AccountChangedBlock> ChangedBlocks { get; set; }

        public required DateTime LastModifiedUtc { get; set; }

        // Observability
        public required string CorrelationId { get; set; }
        public required string TraceId { get; set; }
        public string SourceSystem = "Salesforce";
        public required string EventProducer { get; set; }
    }
}
