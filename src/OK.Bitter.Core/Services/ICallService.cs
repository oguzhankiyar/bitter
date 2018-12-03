namespace OK.Bitter.Core.Services
{
    public interface ICallService
    {
        bool Call(string token, string message);
    }
}