using Flurl;
using Flurl.Http;
using WorksPad.Assistant.Bot.Protocol.ServerBot;
using Novell.Directory.Ldap;
using SearchPRBot.Lib.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

public class APIConnector
{
    private string base_address {get; set;}
    private string ldap_address {get; set;}
    private int ldap_port {get; set;}
    private string ldap_user { get; set; }
    private string ldap_password {get; set;}
    public APIConnector(APIConfig config)
    {
        base_address = config.baseAddress;
        ldap_address = config.ldapAddress;
        ldap_port = config.ldapPort;
        ldap_user = config.ldapUser;
        ldap_password = config.ldapPassword;
    }
    private IFlurlRequest CreateConnectionString(string appendPath, UserCredentials creds)
    {
        string auth = this.EncodeBase64(creds.Username, creds.Password).ToString();
        return this.base_address.WithHeaders(
            new
            {
                Authorization = $"Basic {auth}",
                Content_Type = "application/json"
            }
         ).AppendPathSegment(appendPath);
    }
    private string EncodeBase64(string login, string pass)
    {
        var matches = Regex.Matches(login, "@(\\S+)");

        foreach (Match match in matches)
        {
            login = login.Replace(match.Value, "");
        }
        return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(login + ":" + pass));
    }
    public async Task<dynamic> GetData(string appendPath, UserCredentials creds, object queryParams)
    {
        IFlurlRequest connectionstring = this.CreateConnectionString(appendPath, creds).SetQueryParams(queryParams);
        var r = await connectionstring.GetJsonAsync();
        return r;
    }
}
