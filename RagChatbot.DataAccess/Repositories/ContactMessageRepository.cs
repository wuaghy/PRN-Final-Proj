using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class ContactMessageRepository : Repository<ContactMessage>, IContactMessageRepository
    {
        public ContactMessageRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
