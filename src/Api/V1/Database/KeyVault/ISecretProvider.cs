namespace BackBuddy.Api.Service.V1.Database.KeyVault
{
    public interface ISecretProvider
    {
        Task<string> GetSecret(string secretName);
        Task SetSecret(string secretName, string secretValue);
        Task DeleteSecret(string secretName);
        string GenerateSecret();
    }
}
