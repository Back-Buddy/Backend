
namespace BackBuddy.Api.Service.V1.Database.KeyVault
{
    public class DevSecretProvider : ISecretProvider
    {
        private const string _secret = "ZGFzaXN0ZWludGVzdHNlY3JldGRhc2lzdGVpbnRlc3RzZWNyZXRkYXNpc3RlaW50ZXN0c2VjcmV0ZGFzaXN0ZWludGVzdHNlY3JldGRhc2lzdGVpbnRlc3RzZWNyZXRkYXNpc3RlaW50ZXN0c2VjcmV0";

        public Task DeleteSecret(string secretName)
        {
            return Task.CompletedTask;
        }

        public string GenerateSecret()
        {
            return _secret;
        }

        public Task<string> GetSecret(string secretName)
        {
            return Task.FromResult(_secret); // Test Secret
        }

        public Task SetSecret(string secretName, string secretValue)
        {
            return Task.CompletedTask;
        }
    }
}
