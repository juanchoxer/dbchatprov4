using Azure.AI.OpenAI;
using Azure.Identity;
using DBChatPro.Models;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Text;
using System.Text.Json;

namespace DBChatPro.Services
{
    // Use this constructor if you're using vanilla OpenAI instead of Azure OpenAI
    // Make sure to update your Program.cs as well
    //public class OpenAIService(OpenAIClient aiClient)

    public class AIService(IConfiguration config)
    {
        IChatClient? aiClient;

        public async Task<AIQuery> GetAISQLQuery(string aiModel, string aiService, string userPrompt, DatabaseSchema dbSchema, string databaseType)
        {
            if (aiClient == null)
            {
                aiClient = CreateChatClient(aiModel, aiService);
            }

            List<ChatMessage> chatHistory = new List<ChatMessage>();
            var builder = new StringBuilder();
            var maxRows = config.GetValue<string>("MAX_ROWS");

            builder.AppendLine("Eres un asistente inteligente de SQL que ayuda a los usuarios a consultar una base de datos de Microsoft SQL Server. Los usuarios describen lo que necesitan en lenguaje natural (español). Tu tarea es generar una consulta SQL (T-SQL) correcta y eficiente en base a la intención del usuario. Usa el siguiente esquema de base de datos:");

            foreach(var table in dbSchema.SchemaRaw)
            {
                builder.AppendLine(table);
            }

            builder.AppendLine("Un cliente puede tener muchas facturas y muchos pagos. Para evitar errores de duplicación de datos (producto cartesiano), no hagas JOIN directo entre múltiples tablas con relaciones uno-a-muchos.");
            builder.AppendLine("Si necesitas sumar datos de Factura y Pago al mismo tiempo, primero usa subconsultas agregadas (GROUP BY + SUM) y luego haz JOIN con Cliente.");
            builder.AppendLine("Incluye encabezados de nombres de columnas en los resultados de la consulta.");
            builder.AppendLine("No inventes columnas ni tablas.Usa únicamente el esquema provisto.");
            builder.AppendLine("Proporcione siempre tu respuesta en el formato JSON a continuación:");
            builder.AppendLine(@"{ ""resumen"": ""tu-resumen"", ""query"":  ""tu-query"" }");
            builder.AppendLine("La salida (output) debe ser solo en formato JSON de una sola línea. No uses caracteres de salto de línea.");
            builder.AppendLine(@"En la respuesta JSON anterior, sustituye ""tu-query"" con la consulta de base de datos utilizada para recuperar los datos solicitados.");
            builder.AppendLine(@"En la respuesta JSON anterior, sustituye ""tu-resumen"" con una explicación de cada paso seguido para crear esta consulta en un párrafo detallado.");
            builder.AppendLine($"Solo usa sintaxis de la base de datos {databaseType}.");
            builder.AppendLine($"Siempre limita la respuesta SQL a {maxRows} filas utilizando SELECT TOP.");// Recuerda que MSSQL usa TOP en vez de LIMIT.");
            //builder.AppendLine("Always include all of the table columns and details.");

            // Build the AI chat/prompts
            chatHistory.Add(new ChatMessage(ChatRole.System, builder.ToString()));
            chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

            // Send request to Azure OpenAI model
            var response = await aiClient!.CompleteAsync(chatHistory);
            var responseContent = response.Message.Text!.Replace("```json", "").Replace("```", "").Replace("\\n", " ");

            try
            {
                return JsonSerializer.Deserialize<AIQuery>(responseContent)!;
            }
            catch (Exception)
            {
                throw new Exception("No se pudo dar el formato correspondiente a la respuesta de la IA. Su respuesta fue: " + response.Message.Text);
            }
        }

        private IChatClient? CreateChatClient(string aiModel, string aiService)
        {
            switch (aiService)
            {
                case "AzureOpenAI":
                    return new AzureOpenAIClient(
                            new Uri(config.GetValue<string>("AZURE_OPENAI_ENDPOINT")!),
                            new DefaultAzureCredential())
                                .AsChatClient(modelId: aiModel);
                case "OpenAI":
                        return new OpenAIClient(config.GetValue<string>("OPENAI_KEY"))
                                    .AsChatClient(modelId: aiModel);
                case "Ollama":
                        return new OllamaChatClient(config.GetValue<string>("Ollama_ENDPOINT")!, aiModel);
            }

            return null;
        }

        public async Task<ChatCompletion> ChatPrompt(List<ChatMessage> prompt, string aiModel, string aiService)
        {
            if (aiClient == null)
            {
                aiClient = CreateChatClient(aiModel, aiService);
            }

            return (await aiClient!.CompleteAsync(prompt));
        }
    }
}
