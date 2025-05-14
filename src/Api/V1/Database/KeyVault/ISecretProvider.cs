namespace BackBuddy.Api.Service.V1.Database.KeyVault
{
    public interface ISecretProvider
    {
        Task<string> GetSecret(string secretName, CancellationToken cancellationToken = default);
        Task SetSecret(string secretName, string secretValue, CancellationToken cancellationToken = default);
        Task DeleteSecret(string secretName, CancellationToken cancellationToken = default);
        string GenerateSecret();
    }
}
