namespace server.domain.chat;

public interface IGlobalChatRoomRepository
{
    Task<GlobalChatRoom> GetAsync();
    Task SaveAsync(GlobalChatRoom room);
}
