namespace Platform.Blazor.Static

{
    public static class Endpoints
    {
        public static string BaseUrl = "https://localhost:7092/";

        public static string RegisterEndpoint = $"{BaseUrl}api/account/register/";
        public static string LoginEndpoint = $"{BaseUrl}api/account/authenticate/";

    }
}
