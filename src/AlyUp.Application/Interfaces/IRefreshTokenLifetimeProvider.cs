namespace AlyUp.Application.Interfaces;

public interface IRefreshTokenLifetimeProvider
{
    int GetLifetimeInDays();
}
