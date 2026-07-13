using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
