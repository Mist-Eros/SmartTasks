using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartTasks.Shared;

namespace SmartTasks.Server.Services;

public class GeminiService
{
    // System prompt kept as a const for now; may be moved to configuration later.
    private const string SYSTEM_PROMPT = """
        You are a task parser. Given a user's free-text task description, extract structured data.

        Return ONLY valid JSON. No explanation. No markdown fences. No leading or trailing text.

        Schema:
        {
          "title": "short string, the task itself",
          "date": "YYYY-MM-DD or null if no date implied",
          "time": "HH:mm (24-hour) or null if no time implied",
          "priority": "low | medium | high | urgent | critical",
          "tags": ["1-4 short lowercase tags, e.g. work, personal, meeting, errand"]
        }

        Rules:
        - Resolve relative dates using the date given in the user message.
        - "Tomorrow" means the literal next day.
        - "Next X" (where X is a weekday) means the closest future occurrence of X, strictly after today. If X is today's weekday, it means 7 days from now. Example: if today is Thursday, "next Thursday" means the Thursday 7 days later, not today.
        - "X next week" means the occurrence of X in the following calendar week (7 days later than "next X").
        - If no date is implied, use null. Do not invent dates.
        - Priority is one of: low, medium, high, urgent, critical.
          - low: no rush ("whenever", "someday", "no rush")
          - medium: default when no strong signal
          - high: important ("important", "need to", "should")
          - urgent: time-critical ("ASAP", "urgent", "deadline soon")
          - critical: must-not-miss ("life or death", "miss or die", "emergency", "cannot miss", "critical")
        - Tags should be generic categories, not the task title. 1-4 tags max, lowercase.
        - The user message includes the current local date and time. Use BOTH when resolving relative expressions.
        - Durations like "in 2 hours", "in 30 minutes", "in 3 days" are relative to the current time. Example: if current time is 12:20, "in 2 hours" means today at 14:20.
        - "This afternoon" = today at 15:00. "Tonight" = today at 20:00. "This evening" = today at 19:00. "This morning" = today at 09:00.
        - If a duration would cross into the next day (e.g. "in 5 hours" from 22:00 → 03:00 the next day), roll the date forward accordingly.

        Examples:

        Input: "Meeting with Sara next Tuesday at 2pm, high priority"
        Output: {"title":"Meeting with Sara","date":"2026-09-29","time":"14:00","priority":"high","tags":["meeting","sara"]}

        Input: "Buy groceries sometime this week"
        Output: {"title":"Buy groceries","date":null,"time":null,"priority":"medium","tags":["errand","shopping"]}

        Input: "URGENT: submit tax form by Friday"
        Output: {"title":"Submit tax form","date":"2026-09-25","time":null,"priority":"high","tags":["finance","deadline"]}

        Input: "wawa in 2 hours"
        (assuming current time is 2026-09-25 12:20)
        Output: {"title":"Wawa","date":"2026-09-25","time":"14:20","priority":"medium","tags":["errand"]}

        Input: "MISS OR DIE: submit the final exam by Friday 5pm"
        Output: {"title":"Submit final exam","date":"2026-09-25","time":"17:00","priority":"critical","tags":["deadline","exam"]}
        """;

    private const string GeminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";

    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TaskDto> ParseTaskAsync(string userText, CancellationToken ct = default)
    {
        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey not configured");
        }

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model = "gemini-3.5-flash-lite",
            messages = new object[]
            {
                new { role = "system", content = SYSTEM_PROMPT },
                new { role = "user", content = $"Current local date and time: {DateTime.Now:yyyy-MM-dd HH:mm}. Parse this: {userText}" }
            }
        };

        var response = await client.PostAsJsonAsync(GeminiEndpoint, body, ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini call failed: {(int)response.StatusCode} - {raw}");
        }

        string rawContent;
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(raw);
            rawContent = doc.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? "";
        }
        catch (Exception)
        {
            Console.WriteLine($"Raw Gemini response: {raw}");
            throw new InvalidOperationException($"Gemini returned unparseable response: {raw}");
        }

        rawContent = StripFences(rawContent);

        try
        {
            return JsonSerializer.Deserialize<TaskDto>(rawContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Deserialized to null");
        }
        catch (Exception)
        {
            Console.WriteLine($"Raw Gemini response: {rawContent}");
            throw new InvalidOperationException($"Gemini returned unparseable response: {rawContent}");
        }
    }

    private static string StripFences(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```"))
        {
            return trimmed;
        }

        var afterOpening = trimmed[3..];

        var newlineIndex = afterOpening.IndexOf('\n');
        if (newlineIndex >= 0)
        {
            afterOpening = afterOpening[(newlineIndex + 1)..];
        }
        else
        {
            return trimmed;
        }

        if (afterOpening.EndsWith("```"))
        {
            afterOpening = afterOpening[..^3];
        }

        return afterOpening.Trim();
    }
}
