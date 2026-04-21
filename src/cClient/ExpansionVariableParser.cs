namespace cClient;

internal static class ExpansionVariableParser
{
    public static IReadOnlyDictionary<string, string> Parse(
        IEnumerable<string>? envs,
        FileInfo? envFile)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in ParseFromFile(envFile))
        {
            result[key] = value;
        }

        foreach (var (key, value) in ParseFromStrings(envs))
        {
            result[key] = value;
        }

        return result;
    }

    static IReadOnlyDictionary<string, string> ParseFromFile(FileInfo? envFile)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (envFile is null)
        {
            return result;
        }

        if (envFile.Exists == false)
        {
            throw new FileNotFoundException($"Env file not found: {envFile.FullName}", envFile.FullName);
        }

        var lineNumber = 0;
        foreach (var line in File.ReadLines(envFile.FullName))
        {
            lineNumber++;

            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                throw new ArgumentException($"Invalid --env-file line {lineNumber}: '{line}'. Expected KEY=VALUE.");
            }

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            if (key.Length == 0)
            {
                throw new ArgumentException($"Invalid --env-file line {lineNumber}: '{line}'. KEY must not be empty.");
            }

            result[key] = value;
        }

        return result;
    }
    static IReadOnlyDictionary<string, string> ParseFromStrings(IEnumerable<string>? envs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (envs is null)
        {
            return result;
        }

        foreach (var item in envs)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var separator = item.IndexOf('=');
            if (separator <= 0)
            {
                throw new ArgumentException($"Invalid --env value '{item}'. Expected KEY=VALUE.");
            }

            var key = item[..separator].Trim();
            var value = item[(separator + 1)..].Trim();
            if (key.Length == 0)
            {
                throw new ArgumentException($"Invalid --env value '{item}'. KEY must not be empty.");
            }

            result[key] = value;
        }

        return result;
    }
}
