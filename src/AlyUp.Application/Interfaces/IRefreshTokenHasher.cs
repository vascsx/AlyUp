namespace AlyUp.Application.Interfaces;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}