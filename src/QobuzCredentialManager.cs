using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class QobuzCredentialManager
    {
        public async Task<(string appId, string appSecret)> GetQobuzCredentials(string userId, string userAuthToken)
        {
            try
            {
                var bundleUrl = await GetBundleJsUrl();
                var (appId, appSecret) = await GetAppIdAndSecret(bundleUrl);

                var isValid = await VerifyUserCredentials(userId, userAuthToken, appId);
                if (!isValid)
                {
                    throw new Exception("Invalid user credentials");
                }

                return (appId, appSecret);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private async Task<string> GetBundleJsUrl()
        {
            using (var client = new HttpClient())
            {
                var bundleHTML = await client.GetStringAsync("https://play.qobuz.com/login");
                var match = Regex.Match(bundleHTML, "<script src=\"(?<bundleJS>\\/resources\\/\\d+\\.\\d+\\.\\d+-[a-z]\\d{3}\\/bundle\\.js)");
                if (match.Success)
                {
                    return "https://play.qobuz.com" + match.Groups["bundleJS"].Value;
                }
                throw new Exception("Unable to find bundle.js URL");
            }
        }

        private async Task<(string appId, string appSecret)> GetAppIdAndSecret(string bundleUrl)
        {
            using (var client = new HttpClient())
            {
                var bundleJs = await client.GetStringAsync(bundleUrl);

                var appIdMatch = Regex.Match(bundleJs, "production:{api\\:{appId:\"(?<appID>.*?)\",appSecret:");
                var appId = appIdMatch.Groups["appID"].Value;

                var infoExtrasMatch = Regex.Match(bundleJs, "name:\"[A-Za-z\\/]+\\/Berlin\",info:\"(?<info>[\\w=]+)\",extras:\"(?<extras>[\\w=]+)\"");
                var info = infoExtrasMatch.Groups["info"].Value;
                var extras = infoExtrasMatch.Groups["extras"].Value;

                var seedMatch = Regex.Match(bundleJs, "[a-z]\\.initialSeed\\(\"(?<seed>[\\w=]+)\",window\\.utimezone\\.berlin\\)");
                var seed = seedMatch.Groups["seed"].Value;

                var appSecret = DecodeAppSecret(seed, info, extras);

                return (appId, appSecret);
            }
        }

        private string DecodeAppSecret(string seed, string info, string extras)
        {
            string step1 = seed + info + extras;
            step1 = step1.Substring(0, step1.Length - 44);

            byte[] step1Bytes = Encoding.UTF8.GetBytes(step1);
            string step2 = Convert.ToBase64String(step1Bytes);

            byte[] step2Data = Convert.FromBase64String(step2);
            string step3 = Encoding.UTF8.GetString(step2Data);

            byte[] step3Data = Convert.FromBase64String(step3);
            return Encoding.UTF8.GetString(step3Data);
        }

        private async Task<bool> VerifyUserCredentials(string userId, string userAuthToken, string appId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:67.0) Gecko/20100101 Firefox/67.0");
                var url = $"http://www.qobuz.com/api.json/0.2/user/get?user_id={userId}&user_auth_token={userAuthToken}&app_id={appId}";
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
        }
    }
}
