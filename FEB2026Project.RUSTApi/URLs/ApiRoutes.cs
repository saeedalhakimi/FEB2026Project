namespace FEB2026Project.RUSTApi.URLs
{
    public static class ApiRoutes
    {
        public static class AuthRoutes
        {
            public const string BaseRoute = "api/v{version:apiVersion}/auth";
            public const string Login = "login";
            public const string Register = "register";
            public const string Logout = "logout";
            public const string RefreshToken = "refresh-token";
        }

        public static class RoleRoutes
        {
            public const string BaseRoute = "api/v{version:apiVersion}/roles";
            public const string CreateRole = "create";
            //public const string GetAllRoles = "all";
            public const string GetRoleById = "{id}";
            public const string UpdateRole = "update/{id}";
            public const string DeleteRole = "delete/{id}";
        }
    }
}
