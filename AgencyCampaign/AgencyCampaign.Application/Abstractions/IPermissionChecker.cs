namespace AgencyCampaign.Application.Abstractions
{
    public interface IPermissionChecker
    {
        bool IsRoot { get; }

        bool HasPermission(string permission);

        bool HasAny(params string[] permissions);
    }
}
