using System;
using System.Collections.Generic;
using System.Linq;

public class IconShuffler
{
    public static List<string> GenerateIcons(List<string> iconNames, int countPerIcon)
    {
        // Step 1: Create the input list
        var input = new List<string>();
        foreach (var icon in iconNames)
        {
            for (int i = 0; i < countPerIcon; i++)
                input.Add(icon);
        }

        // Step 2: Shuffle with neighbor check
        var freq = input.GroupBy(x => x)
                        .ToDictionary(g => g.Key, g => g.Count());

        var output = new List<string>();
        string last = null;

        while (output.Count < input.Count)
        {
            var candidates = freq
                .Where(kv => kv.Value > 0 && kv.Key != last)
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            if (candidates.Count == 0)
            {
                // Only option left is to repeat last one
                var fallback = freq.First(kv => kv.Value > 0).Key;
                output.Add(fallback);
                freq[fallback]--;
                last = fallback;
            }
            else
            {
                var pick = candidates[0];
                output.Add(pick);
                freq[pick]--;
                last = pick;
            }
        }

        return output;
    }
}
