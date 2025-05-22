namespace CRUDAPI.Domain.DataSources
{
    public interface IRefreshTokensource
    {
        string GenerateRefreshToken();
    }
}