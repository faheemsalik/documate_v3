
namespace Documate.Data
{
    public static class ProjectSettings
    {
        public static string TempDocsFolder { get; set; } = "D:\\DocumateFiles\\";
        public static string AccessKey { get; } = "";
        public static string SecretKey { get; } = "";
        public static string ApiEndPoint { get; set; }
        public static string NanoApiKey { get; } = "";
        public static string NanoModelId { get; } = "";
        public static string NanoApiEndPoint { get; } = "https://app.nanonets.com/api/v2/";
        public static string OpenAiEndPoint { get; } = "https://api.openai.com/v1/";
        public const int SchdularTimeMinutes = 1; // minutes
    }

}
