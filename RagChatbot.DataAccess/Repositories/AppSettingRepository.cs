using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class AppSettingRepository : Repository<AppSetting>, IAppSettingRepository
    {
        public AppSettingRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
