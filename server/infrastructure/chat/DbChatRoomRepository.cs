using Microsoft.EntityFrameworkCore;
using server.domain.chat;
using server.domain.player;

namespace server.infrastructure.chat
{
    public class DbChatRoomRepository(AppDbContext dbContext) : IChatRoomRepository
    {
        public async Task<ChatRoom> GetChatRoomAsync(PlayerId owner_id)
        {
            var entity = await dbContext.ChatRooms.
            AsNoTracking().
            SingleOrDefaultAsync(x => x.Owner_id == owner_id);
        }        
    }
}
