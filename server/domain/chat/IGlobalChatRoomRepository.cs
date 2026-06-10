namespace server.domain.chat;

public interface IGlobalChatRoomRepository
{
    Task<GlobalChatRoom> GetAsync(int page = 1);
    Task<int> GetTotalCountAsync();
    Task SaveAsync(GlobalChatRoom room);
}
