using UnityEngine;

[CreateAssetMenu(fileName = "New Tile State", menuName = "Tile State")]
public class TileState : ScriptableObject
{
    public Color backgroundColor;
    public Color textColor;

    public int number;
}
