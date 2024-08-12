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

            // Find the start of the Deezer ARLs section
            int startIndex = content.IndexOf("<h3 id=\"deezer-arls\">Deezer ARLs");
            if (startIndex == -1) return new List<string>();

            // Find the start of the table
            startIndex = content.IndexOf("<table class=\"ntable\">", startIndex);
            if (startIndex == -1) return new List<string>();

            // Find the end of the table
            int endIndex = content.IndexOf("</table>", startIndex);
            if (endIndex == -1) return new List<string>();

            // Extract the table content
            string tableContent = content.Substring(startIndex, endIndex - startIndex);

            // Use regex to find all ARL codes
            var matches = Regex.Matches(tableContent, "<td style=\"text-align: left\"><code>(.*?)</code></td>");

            List<string> arls = new List<string>();
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    arls.Add(match.Groups[1].Value);
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
