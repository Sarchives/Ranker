using SQLite;

namespace Ranker
{
    public class SQLiteRankerRepository : IRankerRepository
    {
        private SQLiteConnection db;

        public SQLiteRankerRepository(string path)
        {
            db = new SQLiteConnection(path);
        }

        public IRanksRepository Ranks => new SQLiteRanksRepository(db);

        public IRolesRepository Roles => new SQLiteRolesRepository(db);

        public ISettingsRepository Settings => new SQLiteSettingsRepository(db);
    }
}
