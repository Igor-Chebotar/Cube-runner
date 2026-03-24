using UnityEngine;

[CreateAssetMenu(fileName = "NewObstacle", menuName = "Runner/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    public GameObject prefab;
    public int damage = 10;
    public float spd = 0f;
}
