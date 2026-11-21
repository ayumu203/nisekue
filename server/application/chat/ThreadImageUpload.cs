namespace server.application.chat;

/// <summary>スレッド作成時に添付された画像（再エンコード前）。</summary>
public sealed record ThreadImageUpload(byte[] Content, bool IsSensitive);
