namespace server.application.chat;

/// <summary>画像の保存先が設定されておらず、画像を受け付けられないときに投げる。</summary>
public sealed class ThreadImageUnavailableException()
    : Exception("現在、画像の投稿はできません。");
