using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class DeezerArlScraper
{
    private const string Url = "https://rentry.org/firehawk52";

    public async Task<List<string>> GetAllArlsAsync()
    {
        using (HttpClient httpClient = new HttpClient())
        {
            string content = await httpClient.GetStringAsync(Url);

            // Split the content into lines
            string[] lines = content.Split('\n');

            // Initialize variables to keep track of the state
            bool inDeezerSection = false;
            List<string> arls = new List<string>();

            // Iterate through the lines and extract the required information based on the pattern
            foreach (string line in lines)
            {
                if (line.Contains("id=\"deezer-arls\">Deezer ARLs"))
                {
                    inDeezerSection = true;
                }
                if (line.Contains("id=\"qobuz-arls\">Qobuz ARLs"))
                {
                    inDeezerSection = false;
                    break;
                }
                if (inDeezerSection)
                {
                    if (line.Contains("<td><code>") && line.Contains("</code></td>"))
                    {
                        string code = Regex.Match(line, "<td><code>(.*?)</code></td>").Groups[1].Value;
                        arls.Add(code);
                    }
                }
            }

            return arls;
        }
    }

    public async Task<string> GetRandomArlAsync()
    {
        var arls = await GetAllArlsAsync();
        if (arls.Count == 0) return null;

        Random random = new Random();
        int index = random.Next(arls.Count);
        return arls[index];
    }
}