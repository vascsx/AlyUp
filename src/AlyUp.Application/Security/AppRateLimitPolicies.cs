namespace AlyUp.Application.Security;

public static class AppRateLimitPolicies
{
    public const string AuthLogin = nameof(AuthLogin);
    public const string AuthRefresh = nameof(AuthRefresh);
    public const string AuthLogout = nameof(AuthLogout);
    public const string AuthRegisterClient = nameof(AuthRegisterClient);
}
