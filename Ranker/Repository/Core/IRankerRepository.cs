namespace Ranker
{
    public interface IRankerRepository
    {
        IRanksRepository Ranks { get; }

        IRolesRepository Roles { get; }

        ISettingsRepository Settings { get; }
    }
}
