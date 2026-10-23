using UnityEngine;

// A crew role (plan 4.7): one passive and one starting item. Roles help but are not required.
[CreateAssetMenu(fileName = "NewRole", menuName = "Night Shift/Role")]
public class RoleData : ScriptableObject
{
    public string roleName = "Courier";
    [TextArea] public string description = "";
    public RolePassive passive = RolePassive.None;
    [Tooltip("Given at the start of every shift (can be empty).")]
    public ItemData startingItem;
}
