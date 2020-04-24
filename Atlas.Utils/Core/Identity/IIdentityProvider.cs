namespace Atlas.Utils.Core.Identity
{
    public interface IIdentityProvider
    {
        string GetUserName();
        string GetFriendlyName();
    }
}