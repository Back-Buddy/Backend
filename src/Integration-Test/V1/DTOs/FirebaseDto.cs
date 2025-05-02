namespace BackBuddy.Integration_Test.V1.DTOs
{
    internal class FirebaseDto
    {
        internal record FirebaseLoginResponseDto
        {
            public required string Kind { get; init; }
            public required bool Registered { get; init; }
            public required string Email { get; init; }
            public required string LocalId { get; init; }
            public required string IdToken { get; init; }
            public required string RefreshToken { get; init; }
            public required string ExpiresIn { get; init; }
        }

        internal record FirebaseRegisterResponseDto
        {
            public required string Kind { get; init; }
            public required string LocalId { get; init; }
            public required string Email { get; init; }
        }

        internal record FirebaseUserQueryResponseDto
        {
            public required string RecordsCount { get; init; }
            public required List<FirebaseUserInfoDto> UserInfo { get; init; }
        }

        internal record FirebaseUserInfoDto
        {
            public required string LocalId { get; init; }
            public required string CreatedAt { get; init; }
            public required string LastLoginAt { get; init; }
            public required bool EmailVerified { get; init; }
            public required string Email { get; init; }
            public required string Salt { get; init; }
            public required string PasswordHash { get; init; }
            public required long PasswordUpdatedAt { get; init; }
            public required string ValidSince { get; init; }
            public required List<FirebaseProviderInfoDto> ProviderUserInfo { get; init; }
            public string LastRefreshAt { get; init; }
        }

        internal record FirebaseProviderInfoDto
        {
            public required string ProviderId { get; init; }
            public required string Email { get; init; }
            public required string FederatedId { get; init; }
            public required string RawId { get; init; }
        }
    }
}
