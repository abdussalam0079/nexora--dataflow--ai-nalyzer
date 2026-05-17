using System.Text;
using System.Text.RegularExpressions;
using DataFlow.Core.Themes;
using Markdig;

namespace DataFlow.UI.Helpers;

/// <summary>Renders assistant markdown into a RichTextBox (matches React: headings → bold, no ### symbols).</summary>
public static class ChatMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static void AppendMessage(RichTextBox box, string role, string content)
    {
        var prefix = role switch
        {
            "user" => "You",
            "assistant" => "DataFlow",
            _ => "System"
        };

        box.SelectionColor = role switch
        {
            "user" => DesignTokens.Text,
            "assistant" => DesignTokens.Accent,
            _ => DesignTokens.TextMuted
        };
        box.SelectionFont = new Font(DesignTokens.FontFamily, 10f, FontStyle.Bold);
        box.AppendText($"{prefix}\n");

        if (role == "assistant")
            AppendMarkdown(box, content);
        else
        {
            box.SelectionColor = DesignTokens.Text;
            box.SelectionFont = new Font(DesignTokens.FontFamily, 10f, FontStyle.Regular);
            box.AppendText(content + "\n");
        }

        box.AppendText("\n");
        box.ScrollToCaret();
    }

    private static void AppendMarkdown(RichTextBox box, string markdown)
    {
        var plain = Markdown.ToPlainText(markdown, Pipeline);
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var mono = new Font("Consolas", 9.5f);
        var normal = new Font(DesignTokens.FontFamily, 10f);
        var bold = new Font(DesignTokens.FontFamily, 10f, FontStyle.Bold);

        var inCode = false;
        var codeBuffer = new StringBuilder();

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("```"))
            {
                if (inCode)
                {
                    box.SelectionFont = mono;
                    box.SelectionColor = Color.FromArgb(79, 70, 229);
                    box.SelectionBackColor = Color.FromArgb(243, 244, 246);
                    box.AppendText(codeBuffer + "\n");
                    codeBuffer.Clear();
                    inCode = false;
                }
                else inCode = true;
                continue;
            }

            if (inCode)
            {
                codeBuffer.AppendLine(line);
                continue;
            }

            if (line.StartsWith("### "))
            {
                AppendLine(box, line[4..], bold, DesignTokens.Text);
            }
            else if (line.StartsWith("## "))
            {
                AppendLine(box, line[3..], bold, DesignTokens.Text);
            }
            else if (line.StartsWith("# "))
            {
                AppendLine(box, line[2..], new Font(DesignTokens.FontFamily, 11f, FontStyle.Bold), DesignTokens.Text);
            }
            else if (Regex.IsMatch(line, @"^[-*]\s+"))
            {
                AppendInline(box, "  • " + line[2..].TrimStart(), normal);
            }
            else if (Regex.IsMatch(line, @"^\d+\.\s+"))
            {
                AppendInline(box, "  " + line + "\n", normal);
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                box.AppendText("\n");
            }
            else
            {
                AppendInline(box, line + "\n", normal);
            }
        }

        if (codeBuffer.Length > 0)
        {
            box.SelectionFont = mono;
            box.SelectionColor = DesignTokens.Text;
            box.SelectionBackColor = Color.FromArgb(243, 244, 246);
            box.AppendText(codeBuffer + "\n");
        }

        _ = plain;
    }

    private static void AppendLine(RichTextBox box, string text, Font font, Color color)
    {
        box.SelectionFont = font;
        box.SelectionColor = color;
        box.SelectionBackColor = Color.Transparent;
        box.AppendText(text + "\n");
    }

    private static void AppendInline(RichTextBox box, string text, Font baseFont)
    {
        var parts = Regex.Split(text, @"(\*\*[^*]+\*\*|`[^`]+`)");
        foreach (var part in parts)
        {
            if (part.StartsWith("**") && part.EndsWith("**"))
            {
                box.SelectionFont = new Font(baseFont, FontStyle.Bold);
                box.SelectionColor = DesignTokens.Text;
                box.AppendText(part[2..^2]);
            }
            else if (part.StartsWith('`') && part.EndsWith('`'))
            {
                box.SelectionFont = new Font("Consolas", 9.5f);
                box.SelectionColor = Color.FromArgb(79, 70, 229);
                box.SelectionBackColor = Color.FromArgb(243, 244, 246);
                box.AppendText(part[1..^1]);
                box.SelectionBackColor = Color.Transparent;
            }
            else
            {
                box.SelectionFont = baseFont;
                box.SelectionColor = DesignTokens.Text;
                box.AppendText(part);
            }
        }
    }
}
