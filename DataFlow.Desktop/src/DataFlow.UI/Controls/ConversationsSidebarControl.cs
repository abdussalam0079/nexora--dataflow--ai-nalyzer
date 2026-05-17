using DataFlow.Core.Interfaces;
using DataFlow.Core.Themes;
using DataFlow.UI.Services;

namespace DataFlow.UI.Controls;

public sealed class ConversationsSidebarControl : Panel
{
    private readonly AppShellService _shell;
    private readonly FlowLayoutPanel _list;
    private readonly Label _empty;

    public ConversationsSidebarControl(AppShellService shell)
    {
        _shell = shell;
        Width = DesignTokens.ConversationsWidth;
        Dock = DockStyle.Left;
        BackColor = DesignTokens.NavSecondaryBg;
        Padding = new Padding(0);

        var header = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 12, 8, 0) };
        header.Controls.Add(new Label
        {
            Text = "CONVERSATIONS",
            Dock = DockStyle.Fill,
            ForeColor = DesignTokens.TextDim,
            Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
        var addBtn = new Button
        {
            Text = "+",
            Dock = DockStyle.Right,
            Width = 28,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.AccentBg,
            ForeColor = DesignTokens.Accent,
            Font = new Font(DesignTokens.FontFamily, 12f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        addBtn.FlatAppearance.BorderSize = 0;
        addBtn.Click += (_, _) => _shell.RaiseNewConversation();
        header.Controls.Add(addBtn);

        _empty = new Label
        {
            Dock = DockStyle.Fill,
            Text = "No conversations yet.\nStart a new chat.",
            ForeColor = DesignTokens.TextMuted,
            Font = new Font(DesignTokens.FontFamily, 9f),
            TextAlign = ContentAlignment.TopCenter,
            Padding = new Padding(12, 24, 12, 0)
        };

        _list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8, 4, 8, 8),
            Visible = false
        };

        Controls.Add(_list);
        Controls.Add(_empty);
        Controls.Add(header);

        _shell.ConversationsDataChanged += OnDataChanged;
    }

    private void OnDataChanged(object? sender, ConversationsEventArgs e)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDataChanged(sender, e)); return; }

        if (!string.IsNullOrEmpty(e.EmptyMessage))
        {
            _empty.Text = e.EmptyMessage;
            _empty.Visible = true;
            _list.Visible = false;
            _list.Controls.Clear();
            return;
        }

        if (e.Items is null) return;

        _list.Controls.Clear();
        _empty.Visible = e.Items.Count == 0;
        _list.Visible = e.Items.Count > 0;

        if (e.Items.Count == 0)
        {
            _empty.Text = "No conversations yet.\nStart a new chat.";
            return;
        }

        foreach (var item in e.Items)
        {
            var btn = new Button
            {
                Text = item.Title,
                Width = DesignTokens.ConversationsWidth - 24,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = DesignTokens.Text,
                BackColor = e.SelectedId == item.Id ? DesignTokens.AccentBg : Color.Transparent,
                Font = new Font(DesignTokens.FontFamily, 9f),
                Cursor = Cursors.Hand,
                Tag = item.Id,
                Padding = new Padding(8, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) => _shell.RaiseConversationSelected(item.Id);
            _list.Controls.Add(btn);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _shell.ConversationsDataChanged -= OnDataChanged;
        base.Dispose(disposing);
    }
}
