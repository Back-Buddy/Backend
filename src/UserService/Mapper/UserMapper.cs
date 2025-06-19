using BackBuddy.Core.Library.Users.Dtos;
using Google.Cloud.Firestore;

namespace BackBuddy.User.Service.Mapper
{
    public static class UserMapper
    {
        public static UserDto? ToDto(this DocumentSnapshot documentSnapshot)
        {
            if (!documentSnapshot.TryGetValue("uid", out string uid))
                return null;
            if (!documentSnapshot.TryGetValue("display_name", out string displayName))
                return null;

            documentSnapshot.TryGetValue("photo_url", out string? avatar);
            return new UserDto()
            {
                UserId = uid,
                Username = displayName,
                Avatar = avatar
            };
        }

        public static IEnumerable<UserDto> ToDtos(this QuerySnapshot querySnapshot)
        {
            return querySnapshot.Documents
                .Select(doc => doc.ToDto()).OfType<UserDto>();
        }
    }
}
