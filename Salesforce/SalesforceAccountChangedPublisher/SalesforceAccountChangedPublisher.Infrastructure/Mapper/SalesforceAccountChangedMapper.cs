using Contracts.Salesforce.InternalAccountChanged;
using Contracts.Salesforce.InternalAccountChanged.Enums;
using Newtonsoft.Json;
using Observability.Shared.Contracts;
using Observability.Shared.Helpers;
using SalesforceAccountChangedPublisher.Infrastructure.Mapper;
using SalesforceAccountChangedPublisher.Infrastructure.Salesforce;

public class SalesforceAccountChangedMapper : ISalesforceAccountChangedMapper
{
    private readonly IStructuredLogger _logger;
    private readonly ITracingService _tracingService;

    private static readonly Dictionary<string, AccountEventType> EventTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "created", AccountEventType.Created },
        { "updated", AccountEventType.Updated },
        { "deleted", AccountEventType.Deleted }
    };

    private static readonly Dictionary<string, AccountChangedBlock> FieldPrefixMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "BillingAddress.", AccountChangedBlock.BillingAddress },
        { "DeliveryAddress.", AccountChangedBlock.DeliveryAddress },
        { "BillingInfo.", AccountChangedBlock.BillingInfo },
        { "PrimaryContact.", AccountChangedBlock.PrimaryContact }
    };

    public SalesforceAccountChangedMapper(
        IStructuredLogger logger,
        ITracingService tracingService)
    {
        _logger = logger;
        _tracingService = tracingService;
    }

    public InternalAccountChangedMessage? Map(string json)
    {
        var (source, operation) = CallerInfo.GetCallerClassAndMethod();
        var tracingOperationName = $"{source}.{operation}";

        using var activity = _tracingService.StartActivity(tracingOperationName);

        var dto = DeserializeToDto(json);
        if (dto is null)
        {
            _logger.LogError(source, operation, "Deserialization succeeded but returned null. Possibly mismatched JSON structure.");
            return null;
        }

        if (!Guid.TryParse(dto.AccountId, out var accountId))
        {
            _logger.LogError(source, operation, $"Invalid AccountId: {dto.AccountId}");
            return null;
        }

        var changedBlocks = MapChangedFieldsToBlocks(dto.ChangedFields);
        var eventType = MapEventType(dto.EventType);

        if (changedBlocks.Count == 0 || eventType is null)
        {
            _logger.LogError(source, operation, "Mapping failed due to unknown fields or event type.");
            return null;
        }

        try
        {
            return new InternalAccountChangedMessage
            {
                AccountId = accountId,
                AccountName = dto.AccountName,
                AccountType = dto.AccountType,
                ReplayId = dto.ReplayId,

                BillingAddress = dto.BillingAddress,
                DeliveryAddress = dto.DeliveryAddress,
                BillingInfo = dto.BillingInfo,
                PrimaryContact = dto.PrimaryContact,

                EventType = eventType.Value,
                ChangedBlocks = changedBlocks,
                LastModifiedUtc = dto.LastModifiedDate,

                CorrelationId = _logger.Current.CorrelationId!,
                TraceId = _logger.Current.TraceId!,
                EventProducer = tracingOperationName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(source, operation, $"Error while building InternalAccountChangedMessage: {ex.Message}", ex);
            return null;
        }
    }

    private SalesforceCdcAccountChangedDto? DeserializeToDto(string json)
    {
        var (source, operation) = CallerInfo.GetCallerClassAndMethod();

        try
        {
            return JsonConvert.DeserializeObject<SalesforceCdcAccountChangedDto>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(source, operation, "Failed to deserialize JSON", ex);
            return null;
        }
    }

    private List<AccountChangedBlock> MapChangedFieldsToBlocks(List<string> changedFields)
    {
        var (source, operation) = CallerInfo.GetCallerClassAndMethod();

        var blocks = new HashSet<AccountChangedBlock>();

        foreach (var field in changedFields)
        {
            var matchedPrefix = FieldPrefixMap
                .FirstOrDefault(p => field.StartsWith(p.Key, StringComparison.OrdinalIgnoreCase));

            if (!matchedPrefix.Equals(default(KeyValuePair<string, AccountChangedBlock>)))
            {
                blocks.Add(matchedPrefix.Value);
            }
            else
            {
                var ex = new ArgumentOutOfRangeException(nameof(field), $"Unsupported changedField: {field}");
                _logger.LogError(source, operation, ex.Message, ex);
            }
        }

        return blocks.ToList();
    }

    private AccountEventType? MapEventType(string salesforceEventType)
    {
        var (source, operation) = CallerInfo.GetCallerClassAndMethod();

        if (EventTypeMap.TryGetValue(salesforceEventType, out var mappedType))
            return mappedType;

        var ex = new ArgumentOutOfRangeException(nameof(salesforceEventType), $"Unsupported event type: {salesforceEventType}");
        _logger.LogError(source, operation, ex.Message, ex);
        return null;
    }
}

