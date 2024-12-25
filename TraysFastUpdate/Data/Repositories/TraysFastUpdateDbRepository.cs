using TraysFastUpdate.Data.Common;

namespace TraysFastUpdate.Data.Repositories
{
    public class TraysFastUpdateDbRepository : Repository, ITraysFastUpdateDbRepository
    {
        public TraysFastUpdateDbRepository(TraysFastUpdateDbContext context)
        {
            Context = context;
        }
    }
}

