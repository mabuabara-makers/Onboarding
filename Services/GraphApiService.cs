using System;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

public class GraphApiService
{
    private readonly IConfiguration _configuration;

    public GraphApiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GetAccessToken()
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{_configuration["AzureAdB2C:TenantId"]}/oauth2/v2.0/token");

        var content = new MultipartFormDataContent
        {
            { new StringContent("https://graph.microsoft.com/.default"), "scope" },
            { new StringContent("client_credentials"), "grant_type" },
            { new StringContent(_configuration["AzureAdB2C:ClientSecret"]), "client_secret" },
            { new StringContent(_configuration["AzureAdB2C:ClientId"]), "client_id" }
        };

        request.Content = content;

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error fetching token: {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(responseBody);

        return jsonResponse["access_token"].ToString();
    }

    public async Task<JArray> FetchUsers(string accessToken)
    {
        using var client = new HttpClient();
        var extensionKeyRoles = $"extension_{_configuration["AzureAdB2C:B2CExtensions"]}_Roles";
        var extensionKeyCompany = $"extension_{_configuration["AzureAdB2C:B2CExtensions"]}_Company";
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/users?$select=id,givenName,surname,userPrincipalName,mail,{extensionKeyRoles},{extensionKeyCompany}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error fetching users: {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var usersJObject = JObject.Parse(responseBody);
        var usersArray = (JArray)usersJObject["value"];

        // Mapeo de GUID a Rol
        foreach (var user in usersArray)
        {
            if (user is JObject userObj && userObj.ContainsKey(extensionKeyRoles))
            {
                var roleGuid = user[extensionKeyRoles].ToString();
                var roleName = roleGuid == _configuration["Groups:Admins"] ? "Admins" : "Users";
                user["rol"] = roleName;
            }
            else
            {
                user["rol"] = "Desconocido";
            }
        }

        return usersArray;
    }

    public async Task<JObject> GetUserById(string userId, string token)
    {
        using var client = new HttpClient();
        var extensionKeyRoles = $"extension_{_configuration["AzureAdB2C:B2CExtensions"]}_Roles";
        var extensionKeyCompany = $"extension_{_configuration["AzureAdB2C:B2CExtensions"]}_Company";
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/users/{userId}?$select=id,givenName,surname,userPrincipalName,mail,{extensionKeyRoles},{extensionKeyCompany}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error fetching user: {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var user = JObject.Parse(responseBody);

        // Mapeo de GUID a Rol
        if (user.Property(extensionKeyRoles) != null)
        {
            var roleGuid = user[extensionKeyRoles].ToString();
            var adminGuidConfig = _configuration["Groups:Admins"].ToString();
            var roleName = roleGuid == adminGuidConfig ? "Admins" : "Users";
            user["rol"] = roleName;
        }
        else
        {
            user["rol"] = "Desconocido";
        }

        return user;
    }
    
    
    public async Task<bool> SetUserRole(string userId, string groupId, string accessToken)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, $"https://graph.microsoft.com/v1.0/users/{userId}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var rolId = groupId == "Admins" ? _configuration["Groups:Admins"] : _configuration["Groups:Users"];

        var content = new StringContent($"{{ \"extension_{_configuration["AzureAdB2C:B2CExtensions"]}_Roles\": \"{rolId}\" }}", Encoding.UTF8, "application/json");
        request.Content = content;

        var response = await client.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> SetUserMail(string userId, string email, string accessToken)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, $"https://graph.microsoft.com/v1.0/users/{userId}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        

        var content = new StringContent($"{{ \"mail\": \"{email}\" }}", Encoding.UTF8, "application/json");
        request.Content = content;

        var response = await client.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreateUser(string email, string firstname, string surname, string company, string password, string role, string accessToken)
    {

        var client = new HttpClient();
        var fullname = firstname + ' ' + surname;
        var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        var content = new StringContent($"{{\r\n    \"accountEnabled\": true," +
                                        $"\r\n    \"displayName\": \"{fullname}\"," +
                                        $"\r\n    \"givenName\": \"{fullname}\"," +
                                        $"\r\n    \"mail\": \"{email}\"," +
                                        //$"\r\n    \"Apellidos\": \"{surname}\"," +
                                        $"\r\n    \"extension_7dc3f134d37e4b7783ce99418e7cc703_Company\": \"{company}\"," +
                                        $"\r\n    \"extension_7dc3f134d37e4b7783ce99418e7cc703_Roles\": \"{role}\"," +
                                        $"\r\n    \"identities\": " +
                                        $"[\r\n        {{\r\n            \"signInType\": \"emailAddress\"," +
                                        $"\r\n            \"issuerAssignedId\": \"{email}\"," +
                                        $"\r\n            \"issuer\": \"{_configuration["AzureAdB2C:Domain"]}\"\r\n        }}\r\n    ]," +
                                        $"\r\n    \"passwordProfile\": " +
                                        $"{{\r\n        \"password\": \"{password}\"," +
                                        $"\r\n        \"forceChangePasswordNextSignIn\": false   \r\n    }}," +
                                        $"\r\n    \"passwordPolicies\": \"DisablePasswordExpiration\"\r\n}}", null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Console.WriteLine(await response.Content.ReadAsStringAsync());

        return response.IsSuccessStatusCode;

    }

}
