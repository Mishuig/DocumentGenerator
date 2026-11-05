using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentGenerator.Services;

/// <summary>
/// MVP DOCX engine. Processes text placeholders in paragraphs throughout the package.
/// Replacement text inherits the formatting of the first character of the placeholder.
/// </summary>
public sealed class DocxTemplateEngine
{
    private static readonly Regex PlaceholderRegex =
        new(@"\{\{\s*([a-zA-Z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

    public IReadOnlyList<string> FindPlaceholders(string docxPath)
    {
        using var document = WordprocessingDocument.Open(docxPath, false);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var paragraph in GetAllParagraphs(document))
        {
            var text = string.Concat(paragraph.Descendants<Text>().Select(x => x.Text));
            foreach (Match match in PlaceholderRegex.Matches(text))
                keys.Add(match.Groups[1].Value);
        }

        return keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public void Generate(string templatePath, string outputPath, IReadOnlyDictionary<string, string> values)
    {
        if (!File.Exists(templatePath)) throw new FileNotFoundException("Шаблон не знайдено.", templatePath);

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (string.IsNullOrWhiteSpace(outputDirectory)) throw new InvalidOperationException("Некоректний шлях збереження.");
        Directory.CreateDirectory(outputDirectory);

        var tempPath = Path.Combine(outputDirectory, $".{Guid.NewGuid():N}.tmp.docx");
        try
        {
            File.Copy(templatePath, tempPath, true);
            using (var document = WordprocessingDocument.Open(tempPath, true))
            {
                foreach (var paragraph in GetAllParagraphs(document))
                    ReplaceInParagraph(paragraph, values);

                document.MainDocumentPart?.Document.Save();
            }

            // Replace only after the complete package was successfully written and closed.
            if (File.Exists(outputPath))
            {
                var staged = outputPath + ".new";
                if (File.Exists(staged)) File.Delete(staged);
                if (File.Exists(outputPath + ".bak")) File.Delete(outputPath + ".bak");
                File.Move(tempPath, staged);
                File.Replace(staged, outputPath, outputPath + ".bak", true);
                if (File.Exists(outputPath + ".bak")) File.Delete(outputPath + ".bak");
            }
            else File.Move(tempPath, outputPath);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (File.Exists(outputPath + ".new")) File.Delete(outputPath + ".new");
            throw;
        }
    }

    private static IEnumerable<Paragraph> GetAllParagraphs(WordprocessingDocument document)
    {
        var main = document.MainDocumentPart;
        if (main?.Document is null) throw new InvalidDataException("DOCX не містить основного документа Word.");
        var paragraphs = main.Document.Descendants<Paragraph>()
            .Concat(main.HeaderParts.SelectMany(x => x.Header?.Descendants<Paragraph>() ?? Enumerable.Empty<Paragraph>()))
            .Concat(main.FooterParts.SelectMany(x => x.Footer?.Descendants<Paragraph>() ?? Enumerable.Empty<Paragraph>()))
            .Concat(main.FootnotesPart?.Footnotes?.Descendants<Paragraph>() ?? Enumerable.Empty<Paragraph>())
            .Concat(main.EndnotesPart?.Endnotes?.Descendants<Paragraph>() ?? Enumerable.Empty<Paragraph>());
        return paragraphs.Distinct().ToList();
    }

    private static void ReplaceInParagraph(Paragraph paragraph, IReadOnlyDictionary<string, string> values)
    {
        var textNodes = paragraph.Descendants<Text>().ToList();
        if (textNodes.Count == 0) return;

        var combined = string.Concat(textNodes.Select(x => x.Text));
        var matches = PlaceholderRegex.Matches(combined).Cast<Match>().ToList();

        // Process from right to left so offsets of earlier placeholders remain valid.
        foreach (var match in matches.OrderByDescending(x => x.Index))
        {
            var key = match.Groups[1].Value;
            if (!values.TryGetValue(key, out var replacement)) replacement = "";
            replacement ??= "";
            if (Regex.IsMatch(key, @"(full.?name|pib|fio|person.?name)", RegexOptions.IgnoreCase))
                replacement = replacement.Replace(' ', '\u00A0');
            ReplaceRange(textNodes, match.Index, match.Length, replacement);
        }

        // Keep the number sign attached to the number to avoid line breaks between them.
        var currentText = string.Concat(textNodes.Select(x => x.Text));
        var numberMatches = Regex.Matches(currentText, @"№[ \t]+(?=\d)").Cast<Match>().ToList();
        foreach (var match in numberMatches.OrderByDescending(x => x.Index))
            ReplaceRange(textNodes, match.Index + 1, match.Length - 1, "\u00A0");
    }

    private static void ReplaceRange(IReadOnlyList<Text> nodes, int start, int length, string replacement)
    {
        var end = start + length;
        var cursor = 0;
        var startNode = -1; var endNode = -1;
        var startOffset = 0; var endOffset = 0;

        for (var i = 0; i < nodes.Count; i++)
        {
            var nodeLength = nodes[i].Text?.Length ?? 0;
            var next = cursor + nodeLength;
            if (startNode < 0 && start >= cursor && (start < next || (nodeLength == 0 && start == cursor)))
            { startNode = i; startOffset = start - cursor; }
            if (startNode >= 0 && end >= cursor && end <= next)
            { endNode = i; endOffset = end - cursor; break; }
            cursor = next;
        }
        // A range ending exactly at the end of the paragraph belongs to the last text node.
        if (startNode >= 0 && endNode < 0 && end == cursor && nodes.Count > 0)
        { endNode = nodes.Count - 1; endOffset = nodes[endNode].Text?.Length ?? 0; }
        if (startNode < 0 || endNode < 0) return;

        var first = nodes[startNode].Text ?? string.Empty;
        var last = nodes[endNode].Text ?? string.Empty;
        if (startNode == endNode)
        { nodes[startNode].Text = first[..startOffset] + replacement + first[endOffset..]; PreserveSpace(nodes[startNode]); return; }

        nodes[startNode].Text = first[..startOffset] + replacement;
        PreserveSpace(nodes[startNode]);
        for (var i = startNode + 1; i < endNode; i++) nodes[i].Text = string.Empty;
        nodes[endNode].Text = last[endOffset..];
        PreserveSpace(nodes[endNode]);
    }

    private static void PreserveSpace(Text node)
    {
        if (!string.IsNullOrEmpty(node.Text) &&
            (char.IsWhiteSpace(node.Text[0]) || char.IsWhiteSpace(node.Text[^1])))
            node.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
    }
}
