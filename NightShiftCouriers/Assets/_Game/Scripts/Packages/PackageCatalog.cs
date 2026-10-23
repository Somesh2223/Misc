using UnityEngine;

// The list of all package types. Packages are sent over the network as a number (index into this list).
[CreateAssetMenu(fileName = "PackageCatalog", menuName = "Night Shift/Package Catalog")]
public class PackageCatalog : ScriptableObject
{
    public PackageData[] packages;

    public int IndexOf(PackageData data)
    {
        for (int i = 0; i < packages.Length; i++)
        {
            if (packages[i] == data)
            {
                return i;
            }
        }
        return 0;
    }

    public PackageData Get(int index)
    {
        if (packages == null || packages.Length == 0)
        {
            return null;
        }
        return packages[Mathf.Clamp(index, 0, packages.Length - 1)];
    }
}
