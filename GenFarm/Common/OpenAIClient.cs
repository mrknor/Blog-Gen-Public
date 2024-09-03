using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenFarm.Common
{
    public class OpenAIClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _openAIApiKey;
        private readonly string _assistantId;

        public OpenAIClient(string openAIApiKey, string assistantId)
        {
            _openAIApiKey = openAIApiKey;
            _assistantId = assistantId;
        }

        // Method to create a new thread
        public async Task<string> CreateThreadAsync()
        {
            string createThreadUrl = "https://api.openai.com/v1/threads";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, createThreadUrl);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {_openAIApiKey}");
            httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");

            using (var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage))
            {
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                    JObject threadData = JObject.Parse(jsonResponse);
                    return threadData["id"]?.ToString();
                }
                else
                {
                    Console.WriteLine($"Error creating thread: {httpResponseMessage.ReasonPhrase}");
                    return null;
                }
            }
        }

        // Method to add a message to an existing thread
        public async Task<string> AddMessageToThreadAsync(string threadId, string userMessage)
        {

            string url = $"https://api.openai.com/v1/threads/{threadId}/messages";
            var requestBody = new
            {
                role = "user",
                content = userMessage
            };
            var response = await SendPostRequestAsync(url, requestBody);
            return response?.GetValue("id")?.ToString();
        }

        // Method to create a run in the thread
        public async Task<string> CreateRunAsync(string threadId, string instructions)
        {
            string url = $"https://api.openai.com/v1/threads/{threadId}/runs";
            var requestBody = new { assistant_id = _assistantId, instructions = instructions };

            var response = await SendPostRequestAsync(url, requestBody);
            return response?.GetValue("id")?.ToString();
        }




        // Generate Content for a Header
        public async Task<string> GenerateContentAsync(string header, string prompt, string threadId)
        {
            var detailedPrompt = $@"
                Write a concise and engaging blog section with the following header: '{header}'.
                Focus on: {prompt}.
                Deliver the information in a simplified manner, using bullet points, short paragraphs, and clear, direct language.
                Ensure the content is easy to read and makes trading concepts feel straightforward and approachable.
                Do not include a conclusion section; instead, end the content naturally as part of the discussion.
                Do not return content in JSON format; structure it like a regular blog post with paragraphs, lists, and subheaders as needed.";

            var maxTokens = 500;

            // Send the prompt to the API
            var messageId = await AddMessageToThreadAsync(threadId, detailedPrompt);

            await CreateRunAsync(threadId, "Generate the content for this blog section");

            // Wait for the assistant's response
            var assistantResponse = await WaitForAssistantResponseAsync(threadId, messageId);

            // Check if the response is empty or null
            if (string.IsNullOrEmpty(assistantResponse))
            {
                throw new Exception("The response from the API was empty or null.");
            }

            return assistantResponse;  // Return the content in plain text with HTML formatting
        }


        public async Task<string> WaitForAssistantResponseAsync(string threadId, string messageId)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                // Retrieve the assistant's response from the thread after the specific message ID
                var assistantResponse = await GetAssistantResponseAsync(threadId, messageId);

                if (!string.IsNullOrEmpty(assistantResponse))
                {
                    return assistantResponse;
                }

                // If 10 attempts have been made without success, trigger another run
                if (attempt == 9)
                {
                    await CreateRunAsync(threadId, "Retry content generation due to no response.");
                }

                // Wait before trying again
                await Task.Delay(5000);
            }

            throw new Exception("Assistant response not received after multiple attempts and retries.");
        }

        private async Task<string> GetAssistantResponseAsync(string threadId, string messageId)
        {
            string url = $"https://api.openai.com/v1/threads/{threadId}/messages";
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {_openAIApiKey}");
            httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");

            using (var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage))
            {
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                    JObject responseObject = JObject.Parse(jsonResponse);
                    var messages = responseObject["data"] as JArray;

                    if (messages != null)
                    {
                        bool messageFound = false;
                        foreach (var message in messages)
                        {
                            // Check if the message ID matches the one sent by the user
                            if (message.Next != null && message.Next["id"]?.ToString() == messageId)
                            {
                                messageFound = true;
                            }
                            // If the user message is found, look for the next assistant message
                            if (messageFound && message["role"]?.ToString() == "assistant")
                            {
                                return message["content"]?.FirstOrDefault()?["text"]?["value"]?.ToString();
                            }
                        }
                    }
                }
            }

            return null;
        }



        // Helper method to send a POST request
        private async Task<JObject> SendPostRequestAsync(string url, object requestBody)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {_openAIApiKey}");
            httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");
            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using (var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage))
            {
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<JObject>(jsonResponse);
                }
                else
                {
                    Console.WriteLine($"Error: {httpResponseMessage.ReasonPhrase}");
                    return null;
                }
            }
        }

        // Helper method to send a GET request
        private async Task<JObject> SendGetRequestAsync(string url)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {_openAIApiKey}");
            httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");

            using (var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage))
            {
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<JObject>(jsonResponse);
                }
                else
                {
                    Console.WriteLine($"Error: {httpResponseMessage.ReasonPhrase}");
                    return null;
                }
            }
        }

        // Helper method to parse headers and prompts from the response
        public Dictionary<string, string> ParseHeadersAndPrompts(string content)
        {
            var headersWithPrompts = new Dictionary<string, string>();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var split = line.Split(new[] { ':' }, 2);
                if (split.Length == 2)
                {
                    var header = split[0].Trim();
                    var prompt = split[1].Trim();
                    if (!headersWithPrompts.ContainsKey(header))
                    {
                        headersWithPrompts[header] = prompt;
                    }
                }
            }

            return headersWithPrompts;
        }

        public void Reset()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            _httpClient.DefaultRequestHeaders.Clear();
        }
    }
}
