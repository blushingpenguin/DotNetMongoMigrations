namespace MongoMigrations
{
	using System.Linq;

	public class ExcludeExperimentalMigrations : MigrationFilter
	{
        public static bool HasExperimentalAttribute(Migration migration)
        {
            if (migration == null)
            {
                return false;
            }
            return migration.GetType()
                .GetCustomAttributes(true)
                .OfType<ExperimentalAttribute>()
                .Any();
        }

        public override bool Exclude(Migration migration)
		{
            return HasExperimentalAttribute(migration);
		}
	}
}