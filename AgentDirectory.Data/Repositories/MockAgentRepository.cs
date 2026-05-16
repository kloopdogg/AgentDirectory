using AgentDirectory.Data.Entities;

namespace AgentDirectory.Data.Repositories;

/// <summary>
/// In-memory agent repository for local development and UI prototyping.
/// Enabled via Data:UseMockData = true in appsettings.Development.json.
/// All CRUD operations work against the in-memory list, so the admin panel is fully functional.
/// </summary>
public class MockAgentRepository : IAgentRepository
{
    private readonly List<AgentEntity> _agents = [
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Document Summarizer",
            ShortDescription = "Summarizes documents, PDFs, and long-form text into clear, concise summaries.",
            LongDescription = "Upload any document or paste text, and this agent will produce a structured summary with key points, action items, and a brief executive overview. Supports PDFs, Word documents, and plain text. Works best with documents under 100 pages.",
            EndpointUrl = "https://mock.agents.internal/document-summarizer",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = true,
            SupportsStreaming = true,
            Tags = "[\"summarization\",\"documents\",\"productivity\"]",
            Category = "Productivity",
            IsPublished = true,
            UsageInstructions = "Paste text directly into the chat, or drop a PDF/Word document into the chat area. Ask for a specific format (bullet points, executive summary, etc.) to customise the output.",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Name = "Code Review Assistant",
            ShortDescription = "Reviews code for bugs, security issues, and style improvements with inline suggestions.",
            LongDescription = "Paste code snippets or entire files and this agent will identify potential bugs, security vulnerabilities, performance bottlenecks, and style inconsistencies. Supports most popular languages including TypeScript, C#, Python, Go, and Rust. Provides line-by-line inline comments.",
            EndpointUrl = "https://mock.agents.internal/code-review",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = false,
            SupportsStreaming = true,
            Tags = "[\"code\",\"review\",\"security\",\"engineering\"]",
            Category = "Engineering",
            IsPublished = true,
            UsageInstructions = "Paste your code and describe the context (e.g. language, framework). You can also ask targeted questions like 'Is there an injection risk here?' or 'How can I optimise this loop?'",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Name = "Data Analyst",
            ShortDescription = "Explores, interprets, and visualises data from CSVs, SQL results, and JSON payloads.",
            LongDescription = "Paste CSV data, SQL query results, or JSON arrays and this agent will describe the data shape, compute statistics, identify trends and anomalies, and suggest follow-up analyses. It can also produce Markdown tables and suggest chart types for the data.",
            EndpointUrl = "https://mock.agents.internal/data-analyst",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = false,
            SupportsStreaming = true,
            Tags = "[\"data\",\"analytics\",\"statistics\",\"csv\"]",
            Category = "Analytics",
            IsPublished = true,
            UsageInstructions = "Paste your data directly into the chat. Prefix with the format if non-obvious (e.g. 'Here's a CSV:' or 'SQL result:'). Then ask your question — trends, anomalies, summaries, or specific calculations.",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Name = "Customer Support Bot",
            ShortDescription = "Handles first-line customer enquiries using company knowledge base and tone guidelines.",
            LongDescription = "This agent is trained on company product documentation, FAQ articles, and support tone guidelines. It can answer product questions, help with common troubleshooting steps, and draft responses for support agents to review and send. Escalation triggers are built-in.",
            EndpointUrl = "https://mock.agents.internal/support-bot",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = false,
            SupportsStreaming = true,
            Tags = "[\"support\",\"customer-service\",\"faq\"]",
            Category = "Support",
            IsPublished = true,
            UsageInstructions = "Type a customer query exactly as a customer would write it. The agent will respond in the configured support tone. Add 'draft mode' to get a response suitable for an agent to copy-paste to a customer.",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
            Name = "Meeting Transcriber",
            ShortDescription = "Transcribes meeting recordings and extracts action items, decisions, and key discussion points.",
            LongDescription = "Drop an audio or video recording and this agent will produce a full transcript, then extract structured outputs: who said what, decisions made, action items with owners, and a summary suitable for the meeting minutes. Supports MP3, MP4, and M4A files up to 500 MB.",
            EndpointUrl = "https://mock.agents.internal/meeting-transcriber",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = true,
            SupportsStreaming = true,
            Tags = "[\"meetings\",\"transcription\",\"action-items\",\"audio\"]",
            Category = "Productivity",
            IsPublished = true,
            UsageInstructions = "Drop a recording file into the chat area. Once uploaded, ask 'transcribe this' or 'extract action items'. You can also paste a raw transcript and ask for the same outputs.",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
            Name = "SQL Query Builder",
            ShortDescription = "Converts natural language questions into optimised SQL queries for your data schema.",
            LongDescription = "Describe your database schema (or paste CREATE TABLE statements) and then ask questions in plain English. This agent generates syntactically correct, optimised SQL queries for PostgreSQL, SQL Server, or MySQL. It explains the query logic and highlights any assumptions made.",
            EndpointUrl = "https://mock.agents.internal/sql-query-builder",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = false,
            SupportsStreaming = true,
            Tags = "[\"sql\",\"database\",\"queries\",\"engineering\"]",
            Category = "Engineering",
            IsPublished = true,
            UsageInstructions = "Start by pasting your schema (CREATE TABLE statements work best). Then ask your question in plain English, e.g. 'Show me the top 10 customers by revenue in the last 30 days'. Specify your SQL dialect (PostgreSQL, SQL Server, etc.) if it matters.",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
            Name = "Legal Document Reviewer",
            ShortDescription = "Reviews contracts and legal documents for risk clauses, missing provisions, and plain-English summaries.",
            LongDescription = "Upload a contract, NDA, SLA, or other legal document. This agent highlights potentially risky clauses, flags missing standard provisions, and produces a plain-English summary of obligations, key dates, and liability caps. Not a substitute for qualified legal advice.",
            EndpointUrl = "https://mock.agents.internal/legal-reviewer",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = true,
            SupportsStreaming = true,
            Tags = "[\"legal\",\"contracts\",\"compliance\",\"risk\"]",
            Category = "Legal",
            IsPublished = true,
            UsageInstructions = "Drop a PDF or paste the document text. Ask for a 'risk summary', 'plain English summary', or 'missing clauses check'. For targeted analysis, ask specific questions like 'What are the termination conditions?'",
            CreatedBy = "dev@company.com",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000008"),
            Name = "Image Describer",
            ShortDescription = "Describes, analyses, and extracts information from images, screenshots, and diagrams.",
            LongDescription = "Paste or drop any image — screenshots, diagrams, photos, charts — and this agent will produce a detailed description, extract visible text (OCR), identify UI elements, or answer specific questions about the image content. Useful for accessibility documentation and visual QA.",
            EndpointUrl = "https://mock.agents.internal/image-describer",
            ProtocolType = "Mock",
            AuthType = "None",
            SupportsMultimodal = true,
            SupportsStreaming = true,
            Tags = "[\"images\",\"ocr\",\"vision\",\"accessibility\"]",
            Category = "Creative",
            IsPublished = true,
            UsageInstructions = "Paste or drop an image into the chat. Then ask what you need: 'Describe this image', 'Extract all text', 'What UI components do you see?', or any specific question about the image content.",
            CreatedBy = "dev@company.com",
        },
    ];

    private readonly object _lock = new();

    public Task<List<AgentEntity>> GetPublishedAsync(CancellationToken ct = default)
    {
        lock (_lock) return Task.FromResult(_agents.Where(a => a.IsPublished).OrderBy(a => a.Name).ToList());
    }

    public Task<List<AgentEntity>> GetAllAsync(CancellationToken ct = default)
    {
        lock (_lock) return Task.FromResult(_agents.OrderBy(a => a.Name).ToList());
    }

    public Task<AgentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock) return Task.FromResult(_agents.FirstOrDefault(a => a.Id == id));
    }

    public Task<AgentEntity> CreateAsync(AgentEntity agent, CancellationToken ct = default)
    {
        agent.Id = Guid.NewGuid();
        agent.CreatedAt = DateTime.UtcNow;
        agent.UpdatedAt = DateTime.UtcNow;
        lock (_lock) _agents.Add(agent);
        return Task.FromResult(agent);
    }

    public Task<AgentEntity?> UpdateAsync(AgentEntity agent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var existing = _agents.FirstOrDefault(a => a.Id == agent.Id);
            if (existing is null) return Task.FromResult<AgentEntity?>(null);

            existing.Name = agent.Name;
            existing.ShortDescription = agent.ShortDescription;
            existing.LongDescription = agent.LongDescription;
            existing.EndpointUrl = agent.EndpointUrl;
            existing.ProtocolType = agent.ProtocolType;
            existing.AuthType = agent.AuthType;
            existing.AuthSecretRef = agent.AuthSecretRef;
            existing.SupportsMultimodal = agent.SupportsMultimodal;
            existing.SupportsStreaming = agent.SupportsStreaming;
            existing.Tags = agent.Tags;
            existing.Category = agent.Category;
            existing.IconUrl = agent.IconUrl;
            existing.UsageInstructions = agent.UsageInstructions;
            existing.IsPublished = agent.IsPublished;
            existing.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult<AgentEntity?>(existing);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var existing = _agents.FirstOrDefault(a => a.Id == id);
            if (existing is null) return Task.FromResult(false);
            _agents.Remove(existing);
            return Task.FromResult(true);
        }
    }
}
