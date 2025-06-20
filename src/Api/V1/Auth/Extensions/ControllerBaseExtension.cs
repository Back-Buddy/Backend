using BackBuddy.Core.Library.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackBuddy.Api.Service.V1.Auth.Extensions
{
    public static class ControllerBaseExtension
    {
        public static string GetUserId(this ControllerBase controllerBase)
        {
            ClaimsPrincipal claimsPrincipal = controllerBase.User;
            Claim userIdClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "user_id") ?? throw new UnauthorizedException();
            string userId = userIdClaim.Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException();
            return userId;
        }
    }
}
