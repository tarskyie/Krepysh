using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SDATweb
{
    public class HttpRequestSender
    {
        public async Task<string> SendHTTP(string url, string apiKey, string apiModel, string prompt, string systemPrompt)
        {
            const int RETRIES = 1;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Conversation history: system + user + assistant messages
                var messages = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { ["role"] = "system", ["content"] = systemPrompt },
                    new Dictionary<string, string> { ["role"] = "user", ["content"] = prompt }
                };

                string accumulatedReply = string.Empty;

                try
                {
                    for (int i = 0; i < RETRIES; i++) 
                    {
                        ApiPayload payload = new ApiPayload
                        {
                            model = apiModel,
                            messages = messages,
                            temperature = 0.7
                        };

                        string jsonPayload = JsonSerializer.Serialize(payload, PayloadContext.Default.ApiPayload);
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PostAsync(url, content);
                        response.EnsureSuccessStatusCode();

                        string responseString = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseString);

                        string assistantReply = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString() ?? string.Empty;

                        accumulatedReply += assistantReply;

                        // Add assistant reply to conversation history
                        messages.Add(new Dictionary<string, string>
                        {
                            ["role"] = "assistant",
                            ["content"] = assistantReply
                        });

                        // Check if we got the closing HTML tag
                        if (accumulatedReply.Contains("</html>", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        // Ask the model to continue, preserving context
                        messages.Add(new Dictionary<string, string>
                        {
                            ["role"] = "user",
                            ["content"] = "Please continue from where you left off."
                        });
                    }

                    return accumulatedReply;
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
                    return $"HTTP Request Error: {httpEx.Message}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
        }
    }

    public class ApiPayload
    {
        public string model { get; set; } = "";
        public List<Dictionary<string, string>> messages { get; set; } = new List<Dictionary<string, string>>();
        public double temperature { get; set; } = 0.7;
    }
    [JsonSourceGenerationOptions()]
    [JsonSerializable(typeof(ApiPayload))]
    [JsonSerializable(typeof(List<ApiPayload>))]
    internal partial class PayloadContext : JsonSerializerContext
    {
    }
}
