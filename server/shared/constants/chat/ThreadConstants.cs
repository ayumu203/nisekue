namespace server.shared.constants.chat;

public static class ThreadConstants
{
    public const int TitleMaxLength = 50;
    public const int BodyMaxLength = 500;
    public const int ReplyBodyMaxLength = 250;
    public const int MaxThreadsPerPlayer = 10;
    public const int MaxRepliesPerThread = 100;
    public const int ListPageSize = 30;
    public const int PreviewMaxLength = 100;

    /// <summary>R2 上の画像キーの最大長（threads/{playerId}/{guid}.{ext}）。</summary>
    public const int ImageKeyMaxLength = 200;

    /// <summary>アップロードできる元画像の最大サイズ（バイト）。</summary>
    public const int ImageMaxUploadBytes = 5 * 1024 * 1024;

    /// <summary>保存時に縮小する長辺のピクセル数。</summary>
    public const int ImageMaxLongEdge = 1280;

    /// <summary>デコード時に受け付ける最大ピクセル数（巨大な寸法の画像でメモリを使い切らないため）。</summary>
    public const long ImageMaxDecodedPixels = 40_000_000;

    /// <summary>jpeg を再エンコードするときの品質。</summary>
    public const int ImageJpegQuality = 85;
}
