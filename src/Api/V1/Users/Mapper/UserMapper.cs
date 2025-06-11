using BackBuddy.Api.Service.V1.Users.Dtos;
using Google.Cloud.Firestore;

namespace BackBuddy.Api.Service.V1.Users.Mapper
{
    public static class UserMapper
    {
        public static UserDto? ToDto(this DocumentSnapshot documentSnapshot)
        {
            if (!documentSnapshot.TryGetValue("uid", out string uid))
                return null;
            if (!documentSnapshot.TryGetValue("display_name", out string displayName))
                return null;
            if (!documentSnapshot.TryGetValue("photo_url", out string? avatar))
                avatar = null;

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
                .Select(doc => doc.ToDto())
                .Where(dto => dto != null)!;
        }
    }
}
