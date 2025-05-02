using Azure.Security.KeyVault.Secrets;

namespace BackBuddy.Api.Service.V1.Database.KeyVault
{
    public class KeyVaultSecretProvider([FromKeyedServices(Constants.DEVICE_SECRET)] SecretClient secretClient) : ISecretProvider
    {
        private readonly SecretClient _secretClient = secretClient;

        public async Task<string> GetSecret(string secretName)
        {
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
            return secret.Value;
        }

        public async Task SetSecret(string secretName, string secretValue)
        {
            await _secretClient.SetSecretAsync(secretName, secretValue);
        }
        public async Task DeleteSecret(string secretName)
        {
            await _secretClient.StartDeleteSecretAsync(secretName);
            await _secretClient.PurgeDeletedSecretAsync(secretName);
        }
    }
}
