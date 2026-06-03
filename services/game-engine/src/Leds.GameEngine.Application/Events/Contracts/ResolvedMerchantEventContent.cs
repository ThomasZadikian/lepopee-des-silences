namespace Leds.GameEngine.Application.Events.Contracts;

public sealed record ResolvedMerchantEventContent(
    string EventTemplateKey,
    string EventTemplateVersion,
    IReadOnlyCollection<string> Tags,
    string MerchantProfileKey)
    : ResolvedNodeEventContent(
        ResolvedEventContentKind.Merchant,
        EventTemplateKey,
        EventTemplateVersion,
        Tags);