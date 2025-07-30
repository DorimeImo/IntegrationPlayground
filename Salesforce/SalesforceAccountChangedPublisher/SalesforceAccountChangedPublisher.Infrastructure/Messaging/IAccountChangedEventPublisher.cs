using Contracts.Salesforce.InternalAccountChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceAccountChangedPublisher.Infrastructure.Messaging
{
    public interface IAccountChangedEventPublisher
    {
        Task PublishAsync(InternalAccountChangedMessage message, CancellationToken cancellationToken = default);
    }
}
