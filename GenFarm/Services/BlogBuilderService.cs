using System.Text.Json;
using System.Net.Http;
using System.Text;
using GenFarm.Common;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SystemTextJson = System.Text.Json.JsonSerializer;


namespace GenFarm.Services
{
    public class BlogBuilderService
    {
        private readonly OpenAIClient _openAiService;
        private readonly HttpClient _httpClient;
        private readonly string _jwtToken;
        private readonly BackgroundTaskQueue _taskQueue;

        public BlogBuilderService(OpenAIClient openAiService, HttpClient httpClient, string jwtToken, BackgroundTaskQueue taskQueue)
        {
            _openAiService = openAiService;
            _httpClient = httpClient;
            _jwtToken = jwtToken;
            _taskQueue = taskQueue;
        }

        public async Task BuildBlogAsync(string seoPhrase, Guid taskId)
        {
            // Step 1: Create a new thread for the blog generation process
            var threadId = await _openAiService.CreateThreadAsync();

            // Step 2: Generate headers and prompts for the blog
            var headersAndPrompts = await GenerateHeadersAsync(seoPhrase, threadId);

            //Add Conclusion to end
            headersAndPrompts.Add("Conclusion", "Summarize the main points discussed in the blog and provide a final thought for readers.");

            // Step 3: Generate content based on headers and prompts
            var headersAndContent = await GenerateContentAsync(headersAndPrompts, threadId);

            // Step 4: Create an opening paragraph based on the generated content
            var openingParagraph = await GenerateOpeningParagraphAsync(headersAndContent, threadId);
            headersAndContent = headersAndContent.Prepend(new KeyValuePair<string, string>("Introduction", openingParagraph)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Step 4: Format and send the blog post to WordPress
            var formattedBlog = await CompileAndFormatBlogAsync(headersAndContent, threadId);

            await SendToWordPressAsync(seoPhrase, formattedBlog);
            Reset();
        }

        private async Task<string> GenerateOpeningParagraphAsync(Dictionary<string, string> headersAndContent, string threadId)
        {
            var openingParagraphPrompt = new StringBuilder();
            openingParagraphPrompt.AppendLine("Generate an engaging opening paragraph that introduces the following topics and headers:");

            foreach (var section in headersAndContent.Keys)
            {
                openingParagraphPrompt.AppendLine($"- {section}");
            }

            var messageId = await _openAiService.AddMessageToThreadAsync(threadId, openingParagraphPrompt.ToString());
            await _openAiService.CreateRunAsync(threadId, "Generate opening paragraph");

            var openingParagraph = await _openAiService.WaitForAssistantResponseAsync(threadId, messageId);

            return openingParagraph.Trim('`', '\n');
        }

        private void Reset()
        {
            _openAiService.Reset(); // Reset the OpenAIClient state if needed
        }

        private async Task<Dictionary<string, string>> GenerateContentAsync(Dictionary<string, string> headersAndPrompts, string threadId)
        {
            var contentDict = new Dictionary<string, string>();

            foreach (var header in headersAndPrompts)
            {
                var content = await _openAiService.GenerateContentAsync(header.Key, header.Value, threadId);
                contentDict.Add(header.Key, content);
            }

            return contentDict;
        }

        private async Task<string> CompileAndFormatBlogAsync(Dictionary<string, string> headersAndContent, string threadId)
        {
            var formattedBlogPrompt = new StringBuilder();
            formattedBlogPrompt.AppendLine("Format the following content into a blog using appropriate HTML tags like <h2>, <p>, <h3>, <ul>, <li>, and <strong>. Do not include overarching tags like <html>, <head>, or <body>. Ensure the content starts with headers and uses paragraphs and lists as needed:");

            foreach (var section in headersAndContent)
            {
                formattedBlogPrompt.AppendLine($"Header: {section.Key}");
                formattedBlogPrompt.AppendLine($"Content: {section.Value}");
            }

            var messageId = await _openAiService.AddMessageToThreadAsync(threadId, formattedBlogPrompt.ToString());
            await _openAiService.CreateRunAsync(threadId, "Format blog into HTML");

            var formattedContent = await _openAiService.WaitForAssistantResponseAsync(threadId, messageId);

            // Now, generate the custom CTA based on the content
            var ctaPrompt = "Generate a call to action for the end of this blog. Include a catchy header, Include our brand name 'Zentrix', and mention our AI tools like market analysis, stock sentiment, and trade ideas. Also, add links to our website and Discord community as in the example CTA below:";
            ctaPrompt += @"
                <h2><strong>How Zentrix Can Enhance Your VIX Trading</strong></h2>
                While Zentrix specializes in stock trading signals, our platform’s AI-driven insights can be invaluable when trading the VIX as well. Understanding the overall market sentiment and stock trends can help you make better decisions about when to enter or exit VIX-related trades.
                <h3><strong>Why Use Zentrix’s Stock Signals for VIX Trading?</strong></h3>
                Zentrix provides real-time stock signals based on a combination of technical indicators, sentiment analysis, and machine learning models. These signals can offer valuable insights into market conditions, helping you anticipate changes in volatility. For instance, if Zentrix’s signals suggest a broad market downturn, it could be a timely moment to consider hedging with VIX options or ETFs.

                Moreover, our Discord community offers free stock trading signals and a space to discuss trading strategies, including those related to the VIX. By joining this community, you can gain additional perspectives and support as you navigate the complexities of trading market volatility.
                <h2><strong>Conclusion</strong></h2>
                Trading the VIX offers unique opportunities to profit from market volatility, but it requires a solid understanding of how the VIX works, the available trading instruments, and the associated risks. By following the strategies outlined in this guide and leveraging tools like Zentrix’s AI-driven stock signals, you can improve your chances of success in this challenging but rewarding market.

                Ready to enhance your trading strategy? <span style='text-decoration: underline;'><a href='https://zentrix.ai' target='_new' rel='noopener'>Join Zentrix</a></span> today to access real-time stock trading signals that give you the edge you need. Don’t forget to <span style='text-decoration: underline;'><a href='https://discord.gg/kj42cnGPMU' target='_new' rel='noopener'>join our Discord community</a></span> for free stock signals and more!";

            var ctaMessageId = await _openAiService.AddMessageToThreadAsync(threadId, ctaPrompt);
            await _openAiService.CreateRunAsync(threadId, "Generate custom CTA");

            var ctaContent = await _openAiService.WaitForAssistantResponseAsync(threadId, ctaMessageId);

            // Append the CTA to the cleaned output
            string cleanedOutput = formattedContent.Trim('`', '\n');
            cleanedOutput += ctaContent;

            return cleanedOutput;
        }


        private async Task<Dictionary<string, string>> GenerateHeadersAsync(string seoPhrase, string threadId)
        {
            // Construct the prompt
            var prompt = $"Generate 3 sub-headers for a blog post about {seoPhrase}, and provide a brief prompt for each header. Format the output exactly as '{{header}}:{{prompt}}' with no spaces around the colon. Leave out the curly braces. Ensure each header and prompt pair is on a new line. Only return the headers and respective prompts in your response.";

            // Step 1: Add the prompt message to the thread and get message ID
            var messageId = await _openAiService.AddMessageToThreadAsync(threadId, prompt);

            // Step 2: Create a run to process the message and get the assistant's response
            await _openAiService.CreateRunAsync(threadId, "Generate the headers and prompts");

            // Step 3: Fetch the assistant's response from the thread
            var assistantResponse = await _openAiService.WaitForAssistantResponseAsync(threadId, messageId);

            // Step 4: Parse the assistant's response into headers and prompts
            var headersWithPrompts = _openAiService.ParseHeadersAndPrompts(assistantResponse);

            return headersWithPrompts;
        }

        private async Task SendToWordPressAsync(string seoPhrase, string formattedBlog)
        {
            var wpEndpoint = "https://blog.zentrix.ai/wp-json/wp/v2/posts";
            var postData = new
            {
                title = seoPhrase,
                content = formattedBlog,
                status = "draft"
            };

            var content = new StringContent(SystemTextJson.Serialize(postData), Encoding.UTF8, "application/json");

            // Encode username and application password
            var authenticationString = $"ENTER_STRING";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            // Add the Basic Authentication header
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            var response = await _httpClient.PostAsync(wpEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send blog post to WordPress: {response.ReasonPhrase}");
            }
        }

        public Guid QueueBlogGeneration(string seoPhrase)
        {
            var taskId = Guid.NewGuid();

            _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                await BuildBlogAsync(seoPhrase, taskId);
            }, taskId);

            return taskId;
        }

    }

}

