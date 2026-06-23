using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuakePulse_WebService.Models.Api;
using QuakePulse_WebService.Models.Internal;
using QuakePulse_WebService.Orchestration;

namespace QuakePulse_WebService.Services
{
    public class EarthquakeService : IEarthquakeService
    {
        private readonly IEarthquakeOrchestrator _orchestrator;
        private readonly ILogger<EarthquakeService> _logger;

        public EarthquakeService(IEarthquakeOrchestrator orchestrator, ILogger<EarthquakeService> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        public Task<EarthquakeListResponse> GetEarthquakesAsync(EarthquakeQuery query)
            => _orchestrator.GetEarthquakesAsync(query);

        public Task<EarthquakeDto?> GetEarthquakeByIdAsync(string id)
            => _orchestrator.GetEarthquakeByIdAsync(id);

        public Task<EarthquakeListResponse> GetLatestAsync(int limit)
            => _orchestrator.GetLatestAsync(limit);

        public Task<GeoJsonResponse> GetMapDataAsync(EarthquakeQuery query)
            => _orchestrator.GetMapDataAsync(query);

        public Task<HtmlResponse> GetTableAsync(EarthquakeQuery query)
            => _orchestrator.GetTableAsync(query);

        // --------------------------------------------------------------------
        // POST /api/transform  — rule-based HTML from arbitrary JSON array
        // --------------------------------------------------------------------
        public Task<HtmlResponse> TransformToHtmlAsync(TransformRequest request)
        {
            _logger.LogInformation("[API] TransformToHtmlAsync invoked. Mode={Mode} Rows={Rows}",
                request.Mode, request.Data?.Count ?? 0);

            if (request.Mode.Equals("genai", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[FALLBACK] GenAI mode requested but not configured. Returning placeholder HTML.");
                return Task.FromResult(new HtmlResponse
                {
                    Html = "<p>GenAI transformation is not yet configured.</p>",
                    Source = "genai"
                });
            }

            return Task.FromResult(new HtmlResponse
            {
                Html = BuildGenericHtmlTable(request.Data),
                Source = "rule-based"
            });
        }

        // --------------------------------------------------------------------
        // POST /api/earthquake/assistant  — NLP query parser
        // --------------------------------------------------------------------
        public async Task<AssistantResponse> QueryAssistantAsync(AssistantRequest request)
        {
            _logger.LogInformation("[API] QueryAssistantAsync invoked. RawQuery={RawQuery}", request.Query);

            EarthquakeQuery query;
            Dictionary<string, object?> parsedFilters;
            try
            {
                query = ParseNaturalLanguage(request.Query, out parsedFilters);
                _logger.LogInformation(
                    "[API] Assistant parsed query: Start={Start:yyyy-MM-dd} End={End:yyyy-MM-dd} MinMag={MinMag} MaxMag={MaxMag} Limit={Limit}",
                    query.StartDate, query.EndDate, query.MinMagnitude, query.MaxMagnitude, query.Limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Exception occurred while parsing assistant query: {Message}", ex.Message);
                throw;
            }

            var list = await _orchestrator.GetEarthquakesAsync(query);

            return new AssistantResponse
            {
                ParsedFilters = parsedFilters,
                Data = list.Data
            };
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        private static string BuildGenericHtmlTable(List<JsonElement> rows)
        {
            if (rows == null || rows.Count == 0)
                return "<p>No data provided.</p>";

            var keys = rows
                .Where(r => r.ValueKind == JsonValueKind.Object)
                .SelectMany(r => r.EnumerateObject().Select(p => p.Name))
                .Distinct()
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;font-family:sans-serif;\">");
            sb.AppendLine("  <thead style=\"background:#2c3e50;color:#fff;\"><tr>");
            foreach (var k in keys) sb.Append($"<th>{k}</th>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("  <tbody>");

            foreach (var row in rows)
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                sb.Append("    <tr>");
                foreach (var k in keys)
                {
                    var val = row.TryGetProperty(k, out var prop) ? prop.ToString() : "";
                    sb.Append($"<td>{val}</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("  </tbody></table>");
            return sb.ToString();
        }

        private static EarthquakeQuery ParseNaturalLanguage(string input, out Dictionary<string, object?> filters)
        {
            filters = new Dictionary<string, object?>();
            var text = input.ToLowerInvariant();
            var query = new EarthquakeQuery
            {
                StartDate = DateTime.UtcNow.AddDays(-7).Date,
                EndDate = DateTime.UtcNow.Date,
                SortBy = "date"
            };

            // --- Date parsing ---
            if (text.Contains("today"))
            {
                query.StartDate = DateTime.UtcNow.Date;
                filters["startDate"] = query.StartDate;
            }
            else if (text.Contains("yesterday"))
            {
                query.StartDate = DateTime.UtcNow.AddDays(-1).Date;
                query.EndDate = DateTime.UtcNow.AddDays(-1).Date;
                filters["startDate"] = query.StartDate;
                filters["endDate"] = query.EndDate;
            }
            else if (text.Contains("last month"))
            {
                query.StartDate = DateTime.UtcNow.AddMonths(-1).Date;
                filters["startDate"] = query.StartDate;
            }
            else if (text.Contains("last week"))
            {
                query.StartDate = DateTime.UtcNow.AddDays(-7).Date;
                filters["startDate"] = query.StartDate;
            }
            else
            {
                var daysMatch = Regex.Match(text, @"last\s+(\d+)\s+days?");
                if (daysMatch.Success && int.TryParse(daysMatch.Groups[1].Value, out var days))
                {
                    query.StartDate = DateTime.UtcNow.AddDays(-days).Date;
                    filters["startDate"] = query.StartDate;
                }
            }

            // --- Magnitude parsing ---
            double? minMag = null;

            if (text.Contains("great"))      minMag = 8.0;
            else if (text.Contains("major")) minMag = 7.0;
            else if (text.Contains("strong")) minMag = 5.0;
            else if (text.Contains("moderate")) minMag = 4.0;
            else
            {
                var aboveMatch = Regex.Match(text, @"(?:above|over|magnitude|mag)\s*([\d.]+)");
                if (aboveMatch.Success && double.TryParse(aboveMatch.Groups[1].Value, out var m))
                    minMag = m;
                else
                {
                    var plusMatch = Regex.Match(text, @"([\d.]+)\s*\+");
                    if (plusMatch.Success && double.TryParse(plusMatch.Groups[1].Value, out var mp))
                        minMag = mp;
                }
            }

            if (minMag.HasValue)
            {
                query.MinMagnitude = minMag;
                filters["minMagnitude"] = minMag;
            }

            var belowMatch = Regex.Match(text, @"(?:below|under|less\s+than)\s*([\d.]+)");
            if (belowMatch.Success && double.TryParse(belowMatch.Groups[1].Value, out var maxMag))
            {
                query.MaxMagnitude = maxMag;
                filters["maxMagnitude"] = maxMag;
            }

            // --- Limit ---
            var limitMatch = Regex.Match(text, @"(?:top|first|limit)\s+(\d+)");
            if (limitMatch.Success && int.TryParse(limitMatch.Groups[1].Value, out var lim))
            {
                query.Limit = lim;
                filters["limit"] = lim;
            }

            return query;
        }
    }
}
