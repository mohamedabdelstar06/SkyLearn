using System.Text.RegularExpressions;

namespace SkyLearnApi.Services.TextPipeline
{
    public class LocalSummarizer : ILocalSummarizer
    {
        private readonly HashSet<string> _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "a", "an", "to", "of", "in", "i", "is", "that", "it", "on", "you", "this", "for", 
            "but", "with", "are", "have", "be", "at", "or", "as", "was", "so", "if", "out", "not", "we", 
            "he", "she", "they", "by", "from", "their", "will", "can", "would", "about", "what", "which", 
            "there", "your", "my", "me", "how", "up", "has", "do", "when", "all"
        };

        public string GenerateSummary(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "No content to summarize.";

            // Extract sentences and words
            var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+").Where(s => s.Length > 20).ToList();
            if (sentences.Count <= 5) return text; // If text is already very short, just return it.

            var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var words = Regex.Matches(text, @"\b[\w']+\b");

            // Calculate word frequencies
            foreach (Match match in words)
            {
                var word = match.Value.ToLowerInvariant();
                if (_stopWords.Contains(word) || word.Length <= 2) continue;
                
                if (!wordCounts.ContainsKey(word)) wordCounts[word] = 0;
                wordCounts[word]++;
            }

            // Score sentences
            var sentenceScores = new Dictionary<string, double>();
            foreach (var sentence in sentences)
            {
                double score = 0;
                var sentenceWords = Regex.Matches(sentence, @"\b[\w']+\b");
                foreach (Match match in sentenceWords)
                {
                    var word = match.Value.ToLowerInvariant();
                    if (wordCounts.TryGetValue(word, out int count))
                    {
                        score += count;
                    }
                }
                // Normalize score by sentence length to avoid bias towards long sentences
                sentenceScores[sentence] = score / (sentenceWords.Count > 0 ? sentenceWords.Count : 1);
            }

            // Select top X% (e.g., top 25%) of the sentences
            int topN = Math.Max(3, (int)(sentences.Count * 0.25));
            var topSentences = sentences
                .OrderByDescending(s => sentenceScores.GetValueOrDefault(s))
                .Take(topN)
                .ToList();

            // Reorder sentences back to their original sequence
            var finalSentences = sentences.Where(s => topSentences.Contains(s)).ToList();

            return "📌 **Lecture Summary:**\n" + string.Join(" ", finalSentences) + "\n\n💡 *Note: This summary was generated locally and automatically.*";
        }
    }
}
