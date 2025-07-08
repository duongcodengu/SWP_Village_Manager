using Microsoft.AspNetCore.Http;

namespace Village_Manager.Extensions
{
    public static class SessionExtensions
    {
        public static bool IsAdmin(this ISession session)
        {
            var username = session.GetString("Username");
            var roleId = session.GetInt32("RoleId");

            return !string.IsNullOrEmpty(username) && roleId == 1;
        }

        public static bool IsAuthenticated(this ISession session)
        {
            var username = session.GetString("Username");
            return !string.IsNullOrEmpty(username);
        }
    }
} 