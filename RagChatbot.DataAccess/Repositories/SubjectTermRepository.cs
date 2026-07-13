using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class SubjectTermRepository : Repository<SubjectTerm>, ISubjectTermRepository
    {
        public SubjectTermRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
