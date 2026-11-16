namespace server.application.debug;

/// <summary>
/// デバッグ用の操作（<c>/debug</c> グループ）のエンドポイントに付けるメタデータ。
/// これが付いたエンドポイントで 500 になったときは、ApiExceptionHandler が例外の詳細をメッセージに付ける。
/// </summary>
public sealed class DebugOperationMetadata
{
    public static DebugOperationMetadata Instance { get; } = new();

    private DebugOperationMetadata()
    {
    }
}
