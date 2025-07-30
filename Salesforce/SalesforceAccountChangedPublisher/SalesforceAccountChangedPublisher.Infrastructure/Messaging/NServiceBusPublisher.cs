using Contracts.Salesforce.InternalAccountChanged;
using Observability.Shared.Contracts;
using Observability.Shared.Helpers; 

namespace SalesforceAccountChangedPublisher.Infrastructure.Messaging
{
    public class NServiceBusAccountChangedPublisher : IAccountChangedEventPublisher
    {
        private readonly IMessageSession _messageSession;
        private readonly IStructuredLogger _logger;
        private readonly ITracingService _tracing;

        public NServiceBusAccountChangedPublisher(
            IMessageSession messageSession,
            IStructuredLogger logger,
            ITracingService tracing)
        {
            _messageSession = messageSession;
            _logger = logger;
            _tracing = tracing;
        }

        public async Task PublishAsync(InternalAccountChangedMessage message, CancellationToken cancellationToken)
        {
            var (source, operation) = CallerInfo.GetCallerClassAndMethod();
            var operationName = $"{source}.{operation}";

            using var activity = _tracing.StartActivity(operationName);

            try
            {
                var options = new PublishOptions();
                options.SetHeader("TraceId", message.TraceId);
                options.SetHeader("CorrelationId", message.CorrelationId);

                await _messageSession.Publish(message, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(source, operation, $"Failed to publish message for AccountId={message.AccountId}: {ex.Message}", ex);
                return;
            }
        }
    }
}
