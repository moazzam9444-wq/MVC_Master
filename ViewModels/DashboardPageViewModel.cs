namespace KicksMvc.ViewModels;
// Presentation metadata only. The controller enforces Identity role authorization.
public sealed record DashboardPageViewModel(string Role);
