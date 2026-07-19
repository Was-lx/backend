using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using WaslX.Application.Abstractions.Ai;
using WaslX.Application.Abstractions.Authentication;
using WaslX.Application.Abstractions.Identity;
using WaslX.Application.Features.Escalation.Models;
using WaslX.Application.Abstractions.Knowledge;
using WaslX.Application.Abstractions.Media;
using WaslX.Application.Abstractions.Rag;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Infrastructure.Authentication;
using WaslX.Infrastructure.Email;
using WaslX.Infrastructure.Identity;
using WaslX.Infrastructure.Knowledge.Extraction;
using WaslX.Infrastructure.Knowledge.Pipeline;
using WaslX.Infrastructure.Knowledge.Sources;
using WaslX.Infrastructure.Media;
using WaslX.Infrastructure.Rag;
using WaslX.Infrastructure.Settings;
using WaslX.Infrastructure.WhatsApp;
using WaslX.Infrastructure.AI.Classification;
using WaslX.Application.Abstractions.AI;
using WaslX.Infrastructure.AI;
using WaslX.Infrastructure.Ai.Providers;

namespace WaslX.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<MailSettings>(configuration.GetSection(MailSettings.SectionName));
            services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));
            services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
            services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
            services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
            services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
            services.Configure<ClassificationOptions>(configuration.GetSection("Classification"));
            services.Configure<EscalationScoringOptions>(configuration.GetSection(EscalationScoringOptions.SectionName));

            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IAuthService, AuthSerive>();

            services.AddScoped<IEmailSender, EmailService>();

            // Meta WhatsApp Cloud API client (typed HttpClient via IHttpClientFactory).
            services.AddHttpClient<IMetaGraphApiService, MetaGraphApiService>();

            // OpenAI chat-completion client (typed HttpClient) — powers conversation summaries.
            services.AddHttpClient<IChatCompletionClient, OpenAiChatCompletionClient>();

            services.AddScoped<IMediaStorageService, CloudinaryMediaStorageService>();

            services.AddHttpClient<GroqMessageClassifier>(client =>
            {
                var opts = configuration.GetSection("Classification:Groq").Get<AI.Classification.GroqOptions>();
                if (opts != null)
                {
                    client.BaseAddress = new Uri(opts.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(opts.RequestTimeoutSeconds);
                }
            });

            services.AddTransient<RuleBasedMessageClassifier>();
            services.AddTransient<IMessageClassifier>(sp => sp.GetRequiredService<GroqMessageClassifier>());
            // ---- RAG / AI: provider-agnostic seams → Qdrant ----
            services.AddMemoryCache();

            // Chat runs through Groq directly; embeddings through Hugging Face Inference Providers
            // (BGE-M3). Both are thin, self-contained typed HttpClients — no shared gateway.
            services.AddHttpClient<ILLMProvider, GroqLlmProvider>();
            services.AddHttpClient<IEmbeddingProvider, HuggingFaceEmbeddingProvider>();

            // Qdrant client is thread-safe and long-lived → singleton.
            services.AddSingleton(sp =>
            {
                var q = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
                return string.IsNullOrEmpty(q.ApiKey)
                    ? new QdrantClient(q.Host, q.Port, q.UseTls)
                    : new QdrantClient(q.Host, q.Port, q.UseTls, q.ApiKey);
            });
            services.AddScoped<IVectorStore, QdrantVectorStore>();

            // Ingestion building blocks (source-agnostic; the orchestrator lives in Persistance
            // because it needs direct DbContext access, mirroring WhatsAppWebhookProcessor).
            services.AddSingleton<ITextNormalizer, DefaultTextNormalizer>();
            services.AddSingleton<ITextChunker, TokenTextChunker>();

            // Document source: text extractors (order matters only as a fallback chain) + the
            // source itself (needs an HttpClient to download the Cloudinary-hosted file).
            services.AddSingleton<ITextExtractor, PdfTextExtractor>();
            services.AddSingleton<ITextExtractor, DocxTextExtractor>();
            services.AddSingleton<ITextExtractor, PlainTextExtractor>();
            services.AddHttpClient<IKnowledgeSource, DocumentKnowledgeSource>();

            // Website source: SSRF-guarded fetch (see SsrfGuard) + HTML text extraction.
            services.AddSingleton<IHtmlContentExtractor, AngleSharpHtmlExtractor>();
            services.AddHttpClient<IKnowledgeSource, WebsiteKnowledgeSource>();

            // Retrieval (M6): query embed -> Qdrant ANN -> tenant filter -> diversity rerank.
            services.AddSingleton<IReranker, MmrReranker>();
            services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();

            // Prompt building + orchestration (M7): grounded, cited answer generation.
            services.AddSingleton<IPromptBuilder, PromptBuilder>();
            services.AddScoped<IRagOrchestrator, RagOrchestrator>();

            return services;
        }
    }
}
