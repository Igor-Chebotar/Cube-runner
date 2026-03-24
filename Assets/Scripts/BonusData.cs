using UnityEngine;

public enum BonusType { Heal, SpeedBoost, Invincibility }

[CreateAssetMenu(fileName = "NewBonus", menuName = "Runner/BonusData")]
public class BonusData : ScriptableObject
{
    public BonusType type;
    public float value = 25f;
    public float duration = 5f;
    public Color color = Color.green;
}
