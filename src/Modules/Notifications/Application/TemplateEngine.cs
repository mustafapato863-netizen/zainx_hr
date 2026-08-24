using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Workforce.Modules.Notifications.Application;

public static class TemplateEngine
{
    private static readonly Regex VariableRegex = new(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

    public static string Render(string templateText, string allowedVariablesJson, IDictionary<string, string> variables, bool htmlEncode = true)
    {
        if (string.IsNullOrEmpty(templateText)) return string.Empty;

        var allowedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(allowedVariablesJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(allowedVariablesJson);
                if (list != null)
                {
                    foreach (var item in list) allowedSet.Add(item);
                }
            }
            catch
            {
                // Fallback to strict empty if JSON parsing fails
            }
        }

        return VariableRegex.Replace(templateText, match =>
        {
            var varName = match.Groups[1].Value;

            // Security guard: Only substitute allowlisted variables
            if (allowedSet.Count > 0 && !allowedSet.Contains(varName))
            {
                return match.Value; // Leave unreplaced or redacted
            }

            if (variables != null && variables.TryGetValue(varName, out var rawVal))
            {
                return htmlEncode ? WebUtility.HtmlEncode(rawVal ?? string.Empty) : (rawVal ?? string.Empty);
            }

            return string.Empty;
        });
    }
}
