using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public class SemanticVersion
    {
        public int Major;
        public int Minor;
        public int Patch;

        public static SemanticVersion Parse(string version)
        {
            string[] parts = version.Split('.');
            return new SemanticVersion
            {
                Major = int.Parse(parts[0]),
                Minor = int.Parse(parts[1]),
                Patch = int.Parse(parts[2]),
            };
        }

        public static bool operator<(SemanticVersion a, SemanticVersion b)
        {
            if (a.Major < b.Major)
            {
                return true;
            }

            if (a.Major > b.Major)
            {
                return false;
            }

            if (a.Minor < b.Minor)
            {
                return true;
            }

            if (a.Minor > b.Minor)
            {
                return false;
            }

            if (a.Patch < b.Patch)
            {
                return true;
            }

            return false;
        }

        public static bool operator>(SemanticVersion a, SemanticVersion b)
        {
            if (a.Major > b.Major)
            {
                return true;
            }

            if (a.Major < b.Major)
            {
                return false;
            }

            if (a.Minor > b.Minor)
            {
                return true;
            }

            if (a.Minor < b.Minor)
            {
                return false;
            }

            if (a.Patch > b.Patch)
            {
                return true;
            }

            return false;
        }
        public static bool operator ==(SemanticVersion a, SemanticVersion b)
        {
            return a.Major == b.Major
                && a.Minor == b.Minor
                && a.Patch == b.Patch;
        }

        public static bool operator !=(SemanticVersion a, SemanticVersion b)
        {
            return a.Major != b.Major
                || a.Minor != b.Minor
                || a.Patch != b.Patch;
        }

        public override bool Equals(object obj)
        {
            return obj is SemanticVersion version &&
                Major == version.Major &&
                Minor == version.Minor &&
                Patch == version.Patch;
        }

        public override int GetHashCode()
        {
            int hashCode = -639545495;
            hashCode = hashCode * -1521134295 + Major.GetHashCode();
            hashCode = hashCode * -1521134295 + Minor.GetHashCode();
            hashCode = hashCode * -1521134295 + Patch.GetHashCode();
            return hashCode;
        }
    }
}
