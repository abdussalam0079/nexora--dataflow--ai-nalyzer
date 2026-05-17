namespace DataFlow.Core.Interfaces;

public interface IAppShellService
{
    bool ShowConversationsPanel { get; set; }
    NavSection ActiveNavSection { get; set; }

    event EventHandler? NewConversationRequested;
    event EventHandler<int>? ConversationSelected;

    void SetConversationsEmpty(string message);
    void LoadConversations(IReadOnlyList<ConversationItem> items);
    void SelectConversation(int? id);
}

public enum NavSection { AiWorkspace, Projects }

public sealed class ConversationItem
{
    public int Id { get; init; }
    public string Title { get; init; } = "Untitled";
}
