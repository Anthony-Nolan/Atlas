namespace Atlas.Utils.Core.ApplicationInsights
{
    public interface IIdentityProvider
    {
        string GetUserName();
        string GetFriendlyName();
    }
}