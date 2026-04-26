using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using SmartJobTracker.API.DTOs;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Resume parser that extracts text from PDF/DOC files and identifies key sections.
    /// Includes garbled-text detection so corrupted or scanned PDFs don't produce garbage output.
    /// </summary>
    public class ResumeParserService : IResumeParserService
    {
        public async Task<ResumeParseResultDto> ParseResumeAsync(Stream fileStream, string fileName)
        {
            var result = new ResumeParseResultDto();

            try
            {
                string rawText = await ExtractTextAsync(fileStream, fileName);

                // Sanitise first: strip all garbled / non-printable lines
                string text = SanitizeText(rawText);

                // After sanitisation, if we have almost nothing left the PDF is truly unreadable
                // (scanned image, encrypted, or completely corrupt). Threshold: 80 readable chars.
                if (text.Replace("\n", "").Replace(" ", "").Length < 80)
                {
                    result.Summary = "__UNREADABLE_PDF__"; // sentinel for frontend
                    return result;
                }

                result.FullName  = ExtractName(text);
                result.Email     = ExtractEmail(text);
                result.Phone     = ExtractPhone(text);
                result.Skills    = ExtractSkills(text);
                result.Experience = ExtractExperience(text);
                result.Education = ExtractEducation(text);
                result.Summary   = ExtractSummary(text);
                result.Keywords  = ExtractKeywords(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resume parsing error: {ex.Message}");
            }

            return result;
        }

        // ── Text Extraction ────────────────────────────────────────────────────────

        private async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
        {
            var text = new StringBuilder();

            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var pdfReader = new PdfReader(fileStream);
                    using var pdfDoc = new PdfDocument(pdfReader);
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        var pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i));
                        if (!string.IsNullOrWhiteSpace(pageText))
                            text.AppendLine(pageText);
                    }
                }
                catch
                {
                    fileStream.Position = 0;
                    using var sr = new StreamReader(fileStream, Encoding.UTF8);
                    text.Append(await sr.ReadToEndAsync());
                }
            }
            else
            {
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                text.Append(await reader.ReadToEndAsync());
            }

            return text.ToString();
        }

        // ── Readability Guards ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the text is mostly printable ASCII/Latin characters.
        /// A garbled PDF will have a very high ratio of non-printable / non-ASCII characters.
        /// </summary>
        private static bool IsReadableText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            int printable = text.Count(c =>
                (c >= 32 && c <= 126)    // standard ASCII printable
                || c == '\n' || c == '\r' || c == '\t'
                || (c >= 160 && c <= 255) // extended Latin-1 (accents etc.)
            );

            double ratio = (double)printable / text.Length;
            return ratio >= 0.70; // at least 70% readable characters
        }

        /// <summary>
        /// Returns true if a single line is readable (usable for extraction).
        /// </summary>
        private static bool IsReadableLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (line.Length < 2) return false;

            int printable = line.Count(c => c >= 32 && c <= 255 && c != 127);
            double ratio = (double)printable / line.Length;
            return ratio >= 0.80;
        }

        /// <summary>Removes garbled / unreadable lines from the full text.</summary>
        private static string SanitizeText(string rawText)
        {
            var lines = rawText.Split('\n');
            var clean = lines
                .Where(IsReadableLine)
                // Remove lines that are just symbols or very short garbage
                .Select(l => Regex.Replace(l, @"[^\x20-\x7E\xA0-\xFF\t]", "").Trim())
                .Where(l => l.Length >= 2)
                .ToArray();
            return string.Join("\n", clean);
        }

        // ── Field Extractors ───────────────────────────────────────────────────────

        private static string? ExtractName(string text)
        {
            var lines = text.Split('\n');
            foreach (var line in lines.Take(10))
            {
                var trimmed = line.Trim();
                // Looks like a name: 2-6 words, no digits, no @ or urls, 5-60 chars
                if (trimmed.Length >= 5 && trimmed.Length <= 60
                    && !trimmed.Contains('@')
                    && !trimmed.Contains("http", StringComparison.OrdinalIgnoreCase)
                    && !Regex.IsMatch(trimmed, @"\d")
                    && Regex.IsMatch(trimmed, @"^[A-Za-z\s\.\-']+$"))
                {
                    var wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    if (wordCount >= 2 && wordCount <= 5)
                        return trimmed;
                }
            }
            return null;
        }

        private static string? ExtractEmail(string text)
        {
            var match = Regex.Match(text, @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}");
            return match.Success ? match.Value.ToLower() : null;
        }

        private static string? ExtractPhone(string text)
        {
            // Match international and local phone formats
            var match = Regex.Match(text,
                @"(\+\d{1,3}[\s\-.]?)?\(?\d{2,4}\)?[\s\-.]?\d{3,4}[\s\-.]?\d{3,4}");
            return match.Success ? match.Value.Trim() : null;
        }

        private static List<string> ExtractSkills(string text)
        {
            var skills = new List<string>();

            // Longer / more specific skill keywords first to avoid false positives
            var skillPatterns = new[]
            {
                // Languages
                "ASP\\.NET Core", "ASP\\.NET", "\\.NET", "C#", "C\\+\\+",
                "TypeScript", "JavaScript", "Python", "Java", "Kotlin", "Swift",
                "Go(?:lang)?", "Rust", "Ruby", "PHP", "Scala", "R(?= |,|\\n|$)",

                // Frameworks / Libraries
                "React(?:\\.js)?", "Angular(?:JS)?", "Vue(?:\\.js)?",
                "Node\\.js", "Express\\.js", "Django", "Flask", "FastAPI",
                "Spring Boot", "Spring", "Laravel", "Rails",
                "Blazor", "Entity Framework", "SignalR",

                // Databases
                "SQL Server", "PostgreSQL", "MySQL", "MongoDB", "Redis",
                "Elasticsearch", "Oracle", "SQLite", "CosmosDB",

                // Cloud / DevOps
                "Azure(?! Active)", "AWS", "GCP", "Docker", "Kubernetes",
                "CI/CD", "Terraform", "Jenkins", "GitHub Actions",
                "Azure DevOps", "Linux",

                // Concepts / Tools
                "Microservices", "REST API", "GraphQL", "gRPC",
                "Machine Learning", "Deep Learning", "NLP", "AI",
                "Agile", "Scrum", "Git",
                "HTML5?", "CSS3?", "Tailwind", "Bootstrap", "SASS",
                "Power BI", "Tableau", "Excel",
                "Selenium", "Jest", "NUnit", "xUnit",
            };

            foreach (var pattern in skillPatterns)
            {
                if (Regex.IsMatch(text, @"\b" + pattern + @"\b", RegexOptions.IgnoreCase))
                {
                    // Use the cleaned display name (first alternative, without regex syntax)
                    var display = Regex.Replace(pattern, @"[\(\)\?\+\\]|\\b|(?:\?.*)", "")
                                       .Replace(@"(?:", "").Trim();
                    if (!skills.Any(s => s.Equals(display, StringComparison.OrdinalIgnoreCase)))
                        skills.Add(display);
                }
            }

            return skills;
        }

        private static List<ExperienceDto> ExtractExperience(string text)
        {
            var experiences = new List<ExperienceDto>();
            var lines = text.Split('\n')
                            .Select(l => l.Trim())
                            .Where(l => l.Length > 3)
                            .ToArray();

            var jobTitleWords = new[]
            {
                "Developer", "Engineer", "Architect", "Manager", "Designer",
                "Analyst", "Lead", "Senior", "Junior", "Consultant", "Specialist",
                "Director", "Head of", "VP", "CTO", "Intern"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!IsReadableLine(line)) continue;
                if (line.Length >= 120) continue;

                if (jobTitleWords.Any(jt => line.Contains(jt, StringComparison.OrdinalIgnoreCase)))
                {
                    var exp = new ExperienceDto { Role = line };

                    if (i > 0 && IsReadableLine(lines[i - 1]) && lines[i - 1].Length < 80)
                        exp.Company = lines[i - 1];

                    for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                    {
                        if (IsReadableLine(lines[j]) && Regex.IsMatch(lines[j], @"\d{4}"))
                        {
                            exp.Duration = lines[j];
                            break;
                        }
                    }

                    if (!experiences.Any(e => e.Role == exp.Role))
                        experiences.Add(exp);
                }
            }

            return experiences.Take(5).ToList();
        }

        private static List<EducationDto> ExtractEducation(string text)
        {
            var education = new List<EducationDto>();
            var lines = text.Split('\n')
                            .Select(l => l.Trim())
                            .Where(IsReadableLine)
                            .ToArray();

            // Require multi-word degree phrases to avoid false positives like "BS" in noise
            var degreePatterns = new[]
            {
                @"Bachelor\s+of", @"Master\s+of", @"Doctor\s+of", @"PhD", @"Ph\.D",
                @"B\.Tech", @"M\.Tech", @"B\.Sc", @"M\.Sc", @"BCA", @"MCA",
                @"BBA", @"MBA", @"Bachelor's", @"Master's", @"Diploma\s+in",
                @"B\.E\b", @"M\.E\b", @"Associate\s+Degree"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length > 200) continue;

                bool hasDegree = degreePatterns.Any(p =>
                    Regex.IsMatch(line, p, RegexOptions.IgnoreCase));

                if (!hasDegree) continue;

                var edu = new EducationDto { Degree = line };

                if (i > 0 && IsReadableLine(lines[i - 1]) && lines[i - 1].Length < 100)
                    edu.Institution = lines[i - 1];

                for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                {
                    if (IsReadableLine(lines[j]) && Regex.IsMatch(lines[j], @"\b(19|20)\d{2}\b"))
                    {
                        edu.Year = Regex.Match(lines[j], @"\b(19|20)\d{2}\b").Value;
                        break;
                    }
                }

                if (!education.Any(e => e.Degree == edu.Degree))
                    education.Add(edu);
            }

            return education;
        }

        private static string? ExtractSummary(string text)
        {
            var lines = text.Split('\n')
                            .Select(l => l.Trim())
                            .Where(IsReadableLine)
                            .ToArray();

            var headings = new[]
            {
                "Summary", "Objective", "About", "Profile",
                "Professional Statement", "Career Objective", "Professional Summary"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                bool isHeading = headings.Any(h =>
                    lines[i].Equals(h, StringComparison.OrdinalIgnoreCase)
                    || lines[i].StartsWith(h + " ", StringComparison.OrdinalIgnoreCase));

                if (!isHeading) continue;

                var summaryLines = new List<string>();
                for (int j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
                {
                    if (IsReadableLine(lines[j]) && lines[j].Length > 20)
                        summaryLines.Add(lines[j]);
                    if (summaryLines.Count >= 3) break;
                }

                if (summaryLines.Count > 0)
                    return string.Join(" ", summaryLines);
            }

            return null;
        }

        private static List<string> ExtractKeywords(string text)
        {
            return Regex.Matches(text, @"\b[A-Z][a-zA-Z]{2,}\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Distinct()
                .Where(w => w.Length > 2 && w.Length < 30)
                .Take(20)
                .ToList();
        }
    }
}
