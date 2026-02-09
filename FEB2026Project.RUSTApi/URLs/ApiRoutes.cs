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
    }
}
