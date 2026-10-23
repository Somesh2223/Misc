using UnityEngine;

// All roles. Index 0 should be the plain courier (no role).
[CreateAssetMenu(fileName = "RoleCatalog", menuName = "Night Shift/Role Catalog")]
public class RoleCatalog : ScriptableObject
{
    public RoleData[] roles;

    public RoleData Get(int index)
    {
        if (roles == null || roles.Length == 0)
        {
            return null;
        }
        return roles[Mathf.Clamp(index, 0, roles.Length - 1)];
    }
}
