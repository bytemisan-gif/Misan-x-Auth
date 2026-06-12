using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class MxaAuth
{
    public string Secret { get; }
    public string AppName { get; }
    public string AppVersion { get; }
    public string BaseUrl { get; set; } = "https://misanxauth.qzz.io";

    public string Username { get; private set; }
    public string Subscription { get; private set; }
    public string Expiry { get; private set; }

    private static readonly HttpClient client = new HttpClient();

    public MxaAuth(string secret, string appName, string appVersion)
    {
        Secret = secret;
        AppName = appName;
        AppVersion = appVersion;
    }

    private string GetHwid()
    {
        try
        {
            // Simple persistent HWID using system values
            string hwid = Environment.MachineName + "-" + Environment.UserName + "-" + Environment.ProcessorCount;
            return hwid;
        }
        catch
        {
            return "UNKNOWN-HWID-" + Environment.MachineName;
        }
    }

    private async Task<ApiResponse> PostAsync(string path, object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{BaseUrl}{path}", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = "CONNECTION_ERROR: " + ex.Message };
        }
    }

    public async Task<bool> CheckVersionAsync()
    {
        var resp = await PostAsync("/versioncheck", new
        {
            secret = Secret,
            appName = AppName,
            appVersion = AppVersion
        });
        return resp != null && resp.Success && resp.Message == "VERSION_OK";
    }

    public async Task<ApiResponse> LoginAsync(string username, string password)
    {
        var resp = await PostAsync("/login", new
        {
            username = username,
            password = password,
            secret = Secret,
            appName = AppName,
            appVersion = AppVersion,
            hwid = GetHwid()
        });

        if (resp != null && resp.Success)
        {
            Username = resp.Username;
            Subscription = resp.Subscription;
            Expiry = resp.Expiry;
        }
        return resp;
    }

    public async Task<ApiResponse> RegisterAsync(string username, string password, string licenseKey)
    {
        return await PostAsync("/register", new
        {
            username = username,
            password = password,
            licenseKey = licenseKey,
            secret = Secret,
            appName = AppName,
            appVersion = AppVersion,
            hwid = GetHwid()
        });
    }

    public async Task<string> GetVariableAsync(string varName)
    {
        var resp = await PostAsync("/getvariable", new
        {
            secret = Secret,
            appName = AppName,
            appVersion = AppVersion,
            varName = varName
        });
        return resp != null && resp.Success ? resp.Value : null;
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string Subscription { get; set; }
        public string Expiry { get; set; }
        public string Value { get; set; }
    }
}
