namespace server.domain.chat;

public enum ChatMessageDeleteResult
{
    /// <summary>削除した。</summary>
    Deleted,

    /// <summary>メッセージが存在しない、または削除済み。</summary>
    NotFound,

    /// <summary>投稿者本人ではないため削除できない。</summary>
    Forbidden
}
