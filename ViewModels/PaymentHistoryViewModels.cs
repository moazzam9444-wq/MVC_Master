namespace KicksMvc.ViewModels;
public sealed record PaymentHistoryItem(Guid Id,int? BookingId,int? RegistrationId,string Description,string Payer,
    decimal AmountJod,long AmountUsdCents,decimal UsdPerJod,string Status,bool FinancialReview,DateTimeOffset CreatedAt);
public sealed record PaymentHistoryPage(IReadOnlyList<PaymentHistoryItem> Items,int Total,int Page,int PageSize,
    long PaidUsdCents,int ReviewCount,bool ReviewOnly);
