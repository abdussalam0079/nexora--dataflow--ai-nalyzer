using DataFlow.Core.Models;
using DataFlow.Core.Themes;
using Newtonsoft.Json.Linq;

namespace DataFlow.UI.Dialogs;

public sealed class SchemaDialog : Form
{
    public SchemaDialog(DatasetDto dataset)
    {
        Text = "Dataset schema";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 420);
        BackColor = Color.White;
        Font = new Font(DesignTokens.FontFamily, 9.5f);

        var title = new Label
        {
            Text = dataset.FileName,
            Location = new Point(16, 12),
            AutoSize = true,
            Font = new Font(DesignTokens.FontFamily, 12f, FontStyle.Bold),
            ForeColor = DesignTokens.Text
        };
        var meta = new Label
        {
            Text = $"{dataset.RowCount:N0} rows · {dataset.ColCount} columns",
            Location = new Point(16, 36),
            AutoSize = true,
            ForeColor = DesignTokens.TextMuted
        };

        var grid = new DataGridView
        {
            Location = new Point(16, 64),
            Size = new Size(488, 300),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        grid.Columns.Add("column", "Column");
        grid.Columns.Add("type", "Type");

        try
        {
            var schema = string.IsNullOrWhiteSpace(dataset.SchemaJson)
                ? new JObject()
                : JObject.Parse(dataset.SchemaJson);

            foreach (var prop in schema.Properties().OrderBy(p => p.Name))
            {
                var dtype = prop.Value.Type switch
                {
                    JTokenType.Integer => "integer",
                    JTokenType.Float => "float",
                    JTokenType.Boolean => "boolean",
                    JTokenType.Date => "datetime",
                    _ => prop.Value.ToString()
                };
                grid.Rows.Add(prop.Name, dtype);
            }
        }
        catch
        {
            grid.Rows.Add("(unable to parse schema_json)", "");
        }

        if (grid.Rows.Count == 0)
            grid.Rows.Add("(no schema metadata)", "");

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Location = new Point(424, 372),
            Size = new Size(80, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.Accent,
            ForeColor = Color.White
        };
        close.FlatAppearance.BorderSize = 0;
        AcceptButton = close;

        Controls.AddRange([title, meta, grid, close]);
    }
}
