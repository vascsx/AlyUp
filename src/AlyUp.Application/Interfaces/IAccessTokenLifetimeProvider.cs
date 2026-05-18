namespace AlyUp.Application.Interfaces;

public interface IAccessTokenLifetimeProvider
{
    int GetLifetimeInMinutes();
}
