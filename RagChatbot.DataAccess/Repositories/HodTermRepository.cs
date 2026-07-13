using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class HodTermRepository : Repository<HodTerm>, IHodTermRepository
    {
        public HodTermRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
