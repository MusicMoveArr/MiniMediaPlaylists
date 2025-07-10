using System.Text.RegularExpressions;

namespace MiniMediaPlaylists.Helpers;

public class FuzzyHelper
{
    public static bool ExactNumberMatch(string? value1, string? value2)
    {
        if (string.IsNullOrWhiteSpace(value1) || 
            string.IsNullOrWhiteSpace(value2))
        {
            return false;
        }
        
        string regexPattern = "[0-9]*";
        var value1Match = Regex.Matches(value1, regexPattern)
            .Where(match => !string.IsNullOrWhiteSpace(match.Value))
            .Select(match => long.Parse(match.Value))
            .ToList();
        
        var value2Match = Regex.Matches(value2, regexPattern)
            .Where(match => !string.IsNullOrWhiteSpace(match.Value))
            .Select(match => long.Parse(match.Value))
            .ToList();

        if (value1Match.Count != value2Match.Count)
        {
            return false;
        }

        for (int i = 0; i < value1Match.Count; i++)
        {
            if (value1Match[i] != value2Match[i])
            {
                return false;
            }
        }

        return true;
    }
}