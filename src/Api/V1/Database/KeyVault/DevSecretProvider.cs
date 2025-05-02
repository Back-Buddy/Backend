
namespace BackBuddy.Api.Service.V1.Database.KeyVault
{
    public class DevSecretProvider : ISecretProvider
    {
        public Task DeleteSecret(string secretName)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetSecret(string secretName)
        {
            return Task.FromResult("ZGFzaXN0ZWludGVzdHNlY3JldGRhc2lzdGVpbnRlc3RzZWNyZXRkYXNpc3RlaW50ZXN0c2VjcmV0ZGFzaXN0ZWludGVzdHNlY3JldGRhc2lzdGVpbnRlc3RzZWNyZXRkYXNpc3RlaW50ZXN0c2VjcmV0"); // Test Secret
        }

        public Task SetSecret(string secretName, string secretValue)
        {
            return Task.CompletedTask;
        }
    }
}
