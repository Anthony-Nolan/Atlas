namespace Nova.Utils.Identity
{
    public interface IIdentityProvider
    {
        string GetUserName();
        string GetFriendlyName();
    }
}