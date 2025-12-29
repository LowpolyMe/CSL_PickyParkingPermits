using System;

namespace PickyParking.Domain
{
    
    
    
    
    [Serializable]
    public struct PrefabKey : IEquatable<PrefabKey>
    {
        public string PackageName;
        public string PrefabName;

        public PrefabKey(string packageName, string prefabName)
        {
            PackageName = packageName ?? string.Empty;
            PrefabName = prefabName ?? string.Empty;
        }

        public override string ToString()
        {
            return PackageName + "::" + PrefabName;
        }

        public static PrefabKey Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new PrefabKey(string.Empty, string.Empty);
            }

            string[] parts = value.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length == 1)
            {
                return new PrefabKey(string.Empty, parts[0] ?? string.Empty);
            }

            return new PrefabKey(parts[0] ?? string.Empty, parts[1] ?? string.Empty);
        }

        public bool Equals(PrefabKey other)
        {
            return string.Equals(PackageName, other.PackageName, StringComparison.Ordinal)
                && string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PrefabKey && Equals((PrefabKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + (PackageName != null ? PackageName.GetHashCode() : 0);
                h = (h * 31) + (PrefabName != null ? PrefabName.GetHashCode() : 0);
                return h;
            }
        }

        public static bool operator ==(PrefabKey a, PrefabKey b) { return a.Equals(b); }
        public static bool operator !=(PrefabKey a, PrefabKey b) { return !a.Equals(b); }
    }
}



