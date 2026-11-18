namespace Acme.Ticketing.Domain;

public partial class Show
{
    private partial bool CalculateIsSoldOut() => TicketTiers.Count > 0 && TicketTiers.All(tier => tier.Sold >= tier.Quota);
}
