using System.Runtime.CompilerServices;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Development-only adapter that returns pre-canned responses streamed word-by-word.
/// Enabled when Data:UseMockData = true. Lets you feel the streaming UI without real agents.
/// </summary>
public class MockAgentAdapter : IAgentAdapter
{
    public string Protocol => "Mock";

    private static readonly Dictionary<Guid, string> Responses = new()
    {
        // Document Summarizer
        [Guid.Parse("00000000-0000-0000-0000-000000000001")] =
            "I've analysed the document you provided. Here's a structured summary:\n\n" +
            "**Executive Summary**\nThe document outlines the Q3 strategic roadmap, focusing on three pillars: " +
            "customer retention, product expansion, and operational efficiency.\n\n" +
            "**Key Points**\n- Customer churn reduced by 12% YoY following the onboarding revamp\n" +
            "- Two new product lines approved for development: Analytics Suite and Mobile SDK\n" +
            "- Headcount freeze in non-critical roles until end of Q4\n\n" +
            "**Action Items**\n- Product team to deliver MVP specs by September 15\n" +
            "- Finance to review budget reallocation proposal by end of week\n" +
            "- All team leads to submit Q4 OKRs by September 30\n\n" +
            "Would you like me to expand on any section or reformat the summary?",

        // Code Review Assistant
        [Guid.Parse("00000000-0000-0000-0000-000000000002")] =
            "I've reviewed the code. Here's my analysis:\n\n" +
            "**🔴 Critical Issues**\n" +
            "- **Line 42**: SQL query built with string concatenation — this is a SQL injection risk. " +
            "Use parameterised queries or an ORM instead.\n" +
            "- **Line 87**: `catch (Exception e) {}` silently swallows all exceptions. " +
            "At minimum, log the error before continuing.\n\n" +
            "**🟡 Warnings**\n" +
            "- **Line 23**: `userId` is never null-checked before use. If this can come from user input, add a guard.\n" +
            "- **Lines 55–70**: This loop runs O(n²) — consider using a dictionary lookup instead.\n\n" +
            "**🟢 Suggestions**\n" +
            "- Extract the date formatting logic on line 31 into a shared utility — it's duplicated in three places.\n" +
            "- Variable names like `x`, `temp`, and `flag` make the logic hard to follow.\n\n" +
            "Overall the structure is solid. Address the SQL injection issue before shipping. " +
            "Want me to suggest the fixed version of any specific block?",

        // Data Analyst
        [Guid.Parse("00000000-0000-0000-0000-000000000003")] =
            "Here's my analysis of the dataset:\n\n" +
            "**Dataset Overview**\n- 1,247 rows × 8 columns\n- Date range: Jan 2024 – Jun 2024\n" +
            "- No null values detected; 3 duplicate rows removed\n\n" +
            "**Key Statistics**\n| Metric | Value |\n|---|---|\n" +
            "| Mean revenue | $4,821 |\n| Median revenue | $3,940 |\n" +
            "| Std deviation | $2,103 |\n| Max | $18,450 |\n\n" +
            "**Trends & Anomalies**\n" +
            "- Revenue grew 23% from January to June — steepest growth in March (↑18% MoM)\n" +
            "- Two anomalous spikes on Feb 14 and Apr 3 — both coincide with campaign launches\n" +
            "- The Western region is outperforming forecasts by 31%; Eastern is 8% below target\n\n" +
            "**Suggested Next Steps**\n" +
            "- Drill into the Western region to identify the drivers\n" +
            "- Filter out the campaign spike days before trend modelling\n\n" +
            "Would you like a breakdown by region, or shall I run a correlation analysis?",

        // Customer Support Bot
        [Guid.Parse("00000000-0000-0000-0000-000000000004")] =
            "Thank you for reaching out! I'm happy to help with that.\n\n" +
            "Based on what you've described, this sounds like it may be related to a cached session token. " +
            "Here are the steps that resolve this for most customers:\n\n" +
            "1. **Sign out** of your account completely (don't just close the tab)\n" +
            "2. **Clear your browser cache** — in Chrome: Settings → Privacy → Clear browsing data → Cached images and files\n" +
            "3. **Sign back in** and try the action again\n\n" +
            "If the issue persists after those steps, it may be a permissions issue on your account. " +
            "In that case, your account admin can reset your role from the Admin Portal under Users → Manage Roles.\n\n" +
            "Is there anything else I can help you with today?",

        // Meeting Transcriber
        [Guid.Parse("00000000-0000-0000-0000-000000000005")] =
            "I've processed the recording. Here's what I found:\n\n" +
            "**Meeting Summary — Product Sync (47 mins)**\n" +
            "*Attendees: Sarah (PM), James (Eng Lead), Priya (Design), Tom (QA)*\n\n" +
            "**Decisions Made**\n" +
            "- Launch date moved to October 8 to allow time for accessibility fixes\n" +
            "- Dark mode will ship in v2, not v1\n" +
            "- QA sign-off required before any hotfix is pushed to production\n\n" +
            "**Action Items**\n" +
            "| Owner | Task | Due |\n|---|---|---|\n" +
            "| James | Fix the auth timeout bug | Sep 20 |\n" +
            "| Priya | Deliver final onboarding screens | Sep 18 |\n" +
            "| Tom | Complete regression suite for auth flow | Sep 22 |\n" +
            "| Sarah | Update the stakeholder roadmap deck | Sep 17 |\n\n" +
            "**Full Transcript** available on request. Want me to export the action items as a Markdown checklist?",

        // SQL Query Builder
        [Guid.Parse("00000000-0000-0000-0000-000000000006")] =
            "Based on your schema and question, here's the query:\n\n" +
            "```sql\nSELECT\n    c.customer_name,\n    SUM(o.total_amount) AS total_revenue,\n" +
            "    COUNT(o.id) AS order_count\nFROM orders o\nINNER JOIN customers c ON c.id = o.customer_id\n" +
            "WHERE o.created_at >= DATEADD(day, -30, GETDATE())\n  AND o.status = 'completed'\n" +
            "GROUP BY c.id, c.customer_name\nORDER BY total_revenue DESC\nOFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;\n```\n\n" +
            "**What this does:**\n" +
            "- Joins `orders` to `customers` on the foreign key\n" +
            "- Filters to completed orders in the last 30 days\n" +
            "- Groups and sums revenue per customer\n" +
            "- Returns the top 10 by revenue\n\n" +
            "**Assumptions:** I used `GETDATE()` for SQL Server. If you're on PostgreSQL, replace with `NOW()`. " +
            "Let me know if you want to add a revenue threshold filter or break this down by product category.",

        // Legal Document Reviewer
        [Guid.Parse("00000000-0000-0000-0000-000000000007")] =
            "I've reviewed the document. Here's my analysis:\n\n" +
            "**⚠️ Risk Clauses to Review**\n" +
            "- **Section 8.2 — Liability Cap**: Capped at 1× the annual contract value. " +
            "For a SaaS product this is unusually low — industry standard is typically 2–3×.\n" +
            "- **Section 11 — Termination for Convenience**: Vendor can terminate with 30 days notice. " +
            "You have no equivalent right — consider negotiating symmetry.\n" +
            "- **Section 14.3 — Data Residency**: No data residency commitment is specified. " +
            "If you're subject to GDPR this needs to be explicit.\n\n" +
            "**✅ Standard Provisions Present**\n" +
            "- Confidentiality (Section 6) — looks standard\n" +
            "- IP ownership (Section 9) — your data remains yours\n" +
            "- Dispute resolution (Section 16) — English law, London arbitration\n\n" +
            "**❌ Missing Provisions**\n" +
            "- No SLA or uptime commitment\n" +
            "- No breach notification timeline\n" +
            "- No audit rights\n\n" +
            "*This is not legal advice. Please have a qualified solicitor review before signing.*",

        // Image Describer
        [Guid.Parse("00000000-0000-0000-0000-000000000008")] =
            "Here's my description of the image:\n\n" +
            "**Overview**\nThe image shows a web application dashboard with a dark sidebar navigation on the left " +
            "and a main content area on the right.\n\n" +
            "**UI Components Identified**\n" +
            "- Top navigation bar with a search field, notification bell, and user avatar\n" +
            "- Left sidebar with 7 navigation items; 'Analytics' is currently active (highlighted)\n" +
            "- Main area contains a KPI summary row (4 metric cards) followed by a line chart\n" +
            "- A data table below the chart with 5 columns and approximately 12 visible rows\n" +
            "- A floating action button (blue, bottom-right corner)\n\n" +
            "**Extracted Text**\n" +
            "- Header: \"Sales Overview — Q3 2024\"\n" +
            "- Metric cards: \"Revenue $1.2M\", \"Orders 4,821\", \"Avg. Order $249\", \"Churn 2.3%\"\n" +
            "- Table headers: Date, Customer, Product, Amount, Status\n\n" +
            "Would you like an accessibility audit of this screenshot, or shall I describe a specific area in more detail?",
    };

    // Fallback for agents created in the admin panel during mock mode
    private const string FallbackResponse =
        "Hello! I'm a mock agent running in development mode. " +
        "I don't have real capabilities wired up yet, but the streaming UI is working correctly. " +
        "You can see tokens arrive word by word, just as they would from a real agent endpoint.\n\n" +
        "When you connect a real agent endpoint and set the appropriate protocol type, " +
        "this placeholder response will be replaced with the actual agent's output.\n\n" +
        "Is there anything else I can help you explore about the Agent Directory UI?";

    public async IAsyncEnumerable<AgentEvent> StreamMessageAsync(
        string sessionId,
        AgentEntity agent,
        string apiKey,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        IReadOnlyList<UploadedFileInfo> files,
        string? previousResponseId,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var response = Responses.TryGetValue(agent.Id, out var canned) ? canned : FallbackResponse;

        // Simulate a brief "thinking" pause
        await Task.Delay(400, ct);

        // Some agents realistically use tools — emit a tool_call event mid-response
        bool emitToolCall = agent.Id == Guid.Parse("00000000-0000-0000-0000-000000000003")  // Data Analyst
                         || agent.Id == Guid.Parse("00000000-0000-0000-0000-000000000006"); // SQL Query Builder

        var words = response.Split(' ');
        var halfway = words.Length / 2;

        for (int i = 0; i < words.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            // Emit a tool_call event halfway through for applicable agents
            if (emitToolCall && i == halfway)
            {
                yield return new AgentEvent("tool_call", null, agent.Id == Guid.Parse("00000000-0000-0000-0000-000000000006") ? "run_sql_query" : "query_datastore", null);
                await Task.Delay(600, ct);
                yield return new AgentEvent("tool_result", null, null, null);
                await Task.Delay(200, ct);
                emitToolCall = false;
            }

            var token = i < words.Length - 1 ? words[i] + " " : words[i];
            yield return new AgentEvent("token", token, null, null);

            // Vary the delay slightly to feel more natural
            var delay = token.EndsWith(".\n\n") || token.EndsWith(":\n\n") ? 120
                      : token.EndsWith('\n') ? 60
                      : 35;
            await Task.Delay(delay, ct);
        }

        yield return new AgentEvent("done", null, null, null);
    }
}
