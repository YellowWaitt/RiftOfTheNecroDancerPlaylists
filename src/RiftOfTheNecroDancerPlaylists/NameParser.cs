using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RiftOfTheNecroDancerPlaylists.Extension;

namespace RiftOfTheNecroDancerPlaylists;

internal class NameParser
{
    private static readonly string[] Separators = {
        ",", ";", "/", "\\", " ft ", " ft. ", " feat ", " feat. ", " featuring ", " x ", " & ", " + ", " - ", " · "
    };

    private readonly HashSet<string> _seenNames = [];
    private readonly List<(string OriginalInput, string Abbreviation)> _pendingAbbreviations = [];
    private readonly Dictionary<string, HashSet<string>> _matches = [];

    private static string CleanString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string cleaned = Regex.Replace(input, @"[\(\)\[\]\{\}]", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        cleaned = cleaned.ToLowerInvariant();
        return cleaned;
    }

    private static string AddSpaceAfterLastDot(string input)
    {
        int lastDotIndex = input.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex < input.Length - 1 && !char.IsWhiteSpace(input[lastDotIndex + 1]))
        {
            input = input.Insert(lastDotIndex + 1, " ");
        }
        return input;
    }

    private static bool IsAbbreviation(string input)
    {
        return input.Contains('.');
    }

    public void ParseName(string input)
    {
        var cleanedInput = CleanString(input);
        if (string.IsNullOrWhiteSpace(cleanedInput))
        {
            _matches.GetOrCreate(input).Add("");
            return;
        }

        var names = cleanedInput
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select((part) => AddSpaceAfterLastDot(part.Trim()));
        foreach (var name in names)
        {
            if (IsAbbreviation(name))
            {
                _pendingAbbreviations.Add((input, name));
            }
            else
            {
                _seenNames.Add(name);
                _matches.GetOrCreate(input).Add(name);
            }
        }
    }

    public void MatchPendingAbbreviations()
    {
        foreach (var (OriginalInput, Abbreviation) in _pendingAbbreviations)
        {
            var parts = Abbreviation.Split(".").Select((part) => part.Trim()).ToArray();
            if (parts.Count() < 2)
            {
                _matches.GetOrCreate(OriginalInput).Add(Abbreviation);
                continue;
            }

            string lastName = parts[^1];
            string firstLetter = parts[0];
            var foundMatch = false;
            foreach (var seenName in _seenNames)
            {
                string[] seenParts = seenName.Split(' ');
                if (seenParts.Length < 2) continue;
                if (seenParts[0].StartsWith(firstLetter) && seenParts[^1] == lastName)
                {
                    _matches.GetOrCreate(OriginalInput).Add(seenName);
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch) _matches.GetOrCreate(OriginalInput).Add(Abbreviation);
        }
    }

    public HashSet<string> GetMatches(string input)
    {
        return _matches[input];
    }
}
