namespace AlyUp.Application.Security;

public static class AppPolicies
{
    public const string RequireMaster = nameof(RequireMaster);
    public const string RequireSalonOwnerOrMaster = nameof(RequireSalonOwnerOrMaster);
    public const string RequireProfessional = nameof(RequireProfessional);
}
