using DataFlow.Core.Interfaces;

namespace DataFlow.UI.Services;

public sealed class AppShellService : IAppShellService
{
    private bool _showConversations;

    public bool ShowConversationsPanel
    {
        get => _showConversations;
        set
        {
            if (_showConversations == value) return;
            _showConversations = value;
            ConversationsPanelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public NavSection ActiveNavSection { get; set; } = NavSection.AiWorkspace;

    public event EventHandler? NewConversationRequested;
    public event EventHandler<int>? ConversationSelected;

    internal event EventHandler? ConversationsPanelChanged;
    internal event EventHandler? NavSectionChanged;

    public void SetConversationsEmpty(string message) =>
        ConversationsDataChanged?.Invoke(this, new ConversationsEventArgs { EmptyMessage = message });

    public void LoadConversations(IReadOnlyList<ConversationItem> items) =>
        ConversationsDataChanged?.Invoke(this, new ConversationsEventArgs { Items = items });

    public void SelectConversation(int? id) =>
        ConversationsDataChanged?.Invoke(this, new ConversationsEventArgs { SelectedId = id });

    internal event EventHandler<ConversationsEventArgs>? ConversationsDataChanged;

    internal void RaiseNewConversation() => NewConversationRequested?.Invoke(this, EventArgs.Empty);
    internal void RaiseConversationSelected(int id) => ConversationSelected?.Invoke(this, id);

    internal void NotifyShellLayout() => ConversationsPanelChanged?.Invoke(this, EventArgs.Empty);
    internal void NotifyNavSection() => NavSectionChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class ConversationsEventArgs : EventArgs
{
    public string? EmptyMessage { get; init; }
    public IReadOnlyList<ConversationItem>? Items { get; init; }
    public int? SelectedId { get; init; }
}
