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

    private string Rc4(string key, string data)
    {
        int[] s = new int[256];
        for (int i = 0; i < 256; i++) s[i] = i;
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) % 256;
            int temp = s[i]; s[i] = s[j]; s[j] = temp;
        }
        int k = 0;
        int l = 0;
        StringBuilder res = new StringBuilder();
        for (int y = 0; y < data.Length; y++)
        {
            k = (k + 1) % 256;
            l = (l + s[k]) % 256;
            int temp = s[k]; s[k] = s[l]; s[l] = temp;
            res.Append((char)(data[y] ^ s[(s[k] + s[l]) % 256]));
        }
        return res.ToString();
    }

    private string Encrypt(string data, string key)
    {
        string cipher = Rc4(key, data);
        StringBuilder hex = new StringBuilder();
        for (int i = 0; i < cipher.Length; i++)
        {
            hex.Append(((int)cipher[i]).ToString("x2"));
        }
        return hex.ToString();
    }

    private string Decrypt(string hexData, string key)
    {
        StringBuilder data = new StringBuilder();
        for (int i = 0; i < hexData.Length; i += 2)
        {
            data.Append((char)Convert.ToInt32(hexData.Substring(i, 2), 16));
        }
        return Rc4(key, data.ToString());
    }

    private string GetHwid()
    {
        try
        {
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
            var plainJson = JsonSerializer.Serialize(payload);
            var encryptedPayload = Encrypt(plainJson, Secret);
            
            var wrapped = new
            {
                secret = Secret,
                payload = encryptedPayload
            };
            
            var json = JsonSerializer.Serialize(wrapped);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{BaseUrl}{path}", content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            using (JsonDocument doc = JsonDocument.Parse(responseJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("payload", out JsonElement payloadElement))
                {
                    string encryptedResponse = payloadElement.GetString();
                    string decryptedJson = Decrypt(encryptedResponse, Secret);
                    return JsonSerializer.Deserialize<ApiResponse>(decryptedJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            
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
