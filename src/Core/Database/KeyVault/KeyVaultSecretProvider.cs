using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace BackBuddy.Core.Library.Database.KeyVault
{
    public class KeyVaultSecretProvider([FromKeyedServices(Constants.DEVICE_SECRET)] SecretClient secretClient) : ISecretProvider
    {
        private readonly SecretClient _secretClient = secretClient;

        public async Task<string> GetSecret(string secretName, CancellationToken cancellationToken = default)
        {
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            return secret.Value;
        }

        public async Task SetSecret(string secretName, string secretValue, CancellationToken cancellationToken = default)
        {
            await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
        }

        public async Task DeleteSecret(string secretName, CancellationToken cancellationToken = default)
        {
            await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
        }

        public string GenerateSecret()
        {
            byte[] randomBytes = new byte[256];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
