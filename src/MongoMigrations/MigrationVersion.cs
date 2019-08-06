namespace MongoMigrations
{
	using System;

	public struct MigrationVersion : IComparable<MigrationVersion>
	{
        /// <summary>
        /// 	Return the default, "first" version 0.0.0
        /// </summary>
        /// <returns></returns>
        public static MigrationVersion Default { get; } =
            new MigrationVersion(DateTime.MinValue, String.Empty);

        public DateTime Timestamp { get; }
        public string Name { get; }
        // public Migration Migration { get; }

        static int GetComponent(string version, string name, int start, int length, int min, int max)
        {
            string component = version.Substring(start, length);
            if (!Int32.TryParse(component, out int value) || 
                value < min || value > max)
            {
                throw new ArgumentException(
                    $"The {name} component ({component}) of the version ({version}) is invalid", 
                    nameof(version));
            }
            return value;
        }

        public MigrationVersion(string version)
		{
            // e.g. M20190718162300_TestCollectionMigration
            const string minimalVersion = "M20010203040506_N";

            if (String.IsNullOrEmpty(version))
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (version.Length < minimalVersion.Length || version[minimalVersion.Length - 2] != '_')
            {
                throw new ArgumentException(
                    $"The migration version ({version}) must be of the format MyyyyMMddHHmmss_Name");
            }
            if (version[0] != 'M')
            {
                throw new ArgumentException(
                    $"The migration version ({version}) must start with the letter M", nameof(version));
            }
            Timestamp = new DateTime(
                GetComponent(version, "year", 1, 4, 2000, Int32.MaxValue),
                GetComponent(version, "month", 5, 2, 1, 12),
                GetComponent(version, "day", 7, 2, 1, 31),
                GetComponent(version, "hour", 9, 2, 0, 23),
                GetComponent(version, "minute", 11, 2, 0, 59),
                GetComponent(version, "second", 13, 2, 0, 59));
            Name = version.Substring(minimalVersion.Length - 1);
        }

		public MigrationVersion(DateTime timestamp, string name)
		{
            Timestamp = timestamp;
            Name = name;
		}

		public static bool operator ==(MigrationVersion a, MigrationVersion b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(MigrationVersion a, MigrationVersion b)
		{
			return !(a == b);
		}

		public static bool operator <(MigrationVersion a, MigrationVersion b)
		{
            return a.CompareTo(b) < 0;
		}

		public static bool operator >(MigrationVersion a, MigrationVersion b)
		{
            return b < a;
		}

		public static bool operator >=(MigrationVersion a, MigrationVersion b)
		{
			return !(a < b);
		}

		public static bool operator <=(MigrationVersion a, MigrationVersion b)
		{
            return !(b < a);
		}

		public bool Equals(MigrationVersion other)
		{
            return Timestamp == other.Timestamp && Name == other.Name;
		}

		public int CompareTo(MigrationVersion other)
		{
            int ts = Timestamp.CompareTo(other.Timestamp);
            if (ts == 0)
            {
                ts = Name.CompareTo(other.Name);
            }
            return ts;
		}

		public override bool Equals(object obj)
		{
            if (obj == null || obj.GetType() != typeof(MigrationVersion))
            {
                return false;
            }
			return Equals((MigrationVersion) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
                int result = 17;
                result = result * 31 + Timestamp.GetHashCode();
                result = result * 31 + Name.GetHashCode();
				return result;
			}
		}

		public override string ToString()
		{
            return string.Format($"M{Timestamp.ToString("yyyyMMddHHmmss")}_{Name}");
		}
	}
}