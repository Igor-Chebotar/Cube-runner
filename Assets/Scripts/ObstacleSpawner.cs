using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public ObstacleData[] obstacleTypes;
    [SerializeField] float spawnInterval = 2f;
    [SerializeField] float spawnDistance = 50f;
    [SerializeField] float destroyDistance = 20f;
    public float[] lanes = { -3f, 0f, 3f };

    [Header("Бонусы")]
    public BonusData[] bonusTypes;
    public GameObject bonusPrefab;
    [SerializeField] float bonusChance = 0.25f;

    [Header("Земля")]
    [SerializeField] float groundSegmentLength = 200f;
    [SerializeField] Material groundMaterial;

    Transform playerTr;
    float timer;
    List<GameObject> spawnedObjects = new List<GameObject>();

    // для бесконечного пола
    float nextGroundZ;
    List<GameObject> groundSegments = new List<GameObject>();
    int groundLayer;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("Нет объекта с тегом Player!");
            enabled = false;
            return;
        }
        playerTr = playerObj.transform;
        timer = spawnInterval;

        groundLayer = LayerMask.NameToLayer("Ground");

        // запоминаем где начинать докладывать землю
        // (первые два сегмента уже стоят в сцене от автосетапа)
        nextGroundZ = 1200f + groundSegmentLength;
    }

    void Update()
    {
        if (playerTr == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            if (Random.value < bonusChance && bonusTypes.Length > 0)
                SpawnBonus();
            else
                SpawnObstacle();

            timer = spawnInterval;
        }

        // чистим то что уже позади
        CleanupBehind();

        // докладываем землю если игрок подбирается
        SpawnGroundIfNeeded();
    }

    void CleanupBehind()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }
            if (spawnedObjects[i].transform.position.z < playerTr.position.z - destroyDistance)
            {
                Destroy(spawnedObjects[i]);
                spawnedObjects.RemoveAt(i);
            }
        }

        // убираем старые сегменты земли
        for (int i = groundSegments.Count - 1; i >= 0; i--)
        {
            if (groundSegments[i] == null)
            {
                groundSegments.RemoveAt(i);
                continue;
            }
            if (groundSegments[i].transform.position.z < playerTr.position.z - groundSegmentLength * 2)
            {
                Destroy(groundSegments[i]);
                groundSegments.RemoveAt(i);
            }
        }
    }

    void SpawnGroundIfNeeded()
    {
        // если игрок ближе чем 2 сегмента до конца — добавляем ещё
        while (nextGroundZ < playerTr.position.z + groundSegmentLength * 3)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Plane);
            seg.name = "Ground_dyn";
            seg.transform.position = new Vector3(0, 0, nextGroundZ);
            seg.transform.localScale = new Vector3(1, 1, groundSegmentLength / 10f);

            if (groundLayer >= 0)
                seg.layer = groundLayer;
            if (groundMaterial != null)
                seg.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            groundSegments.Add(seg);
            nextGroundZ += groundSegmentLength;
        }
    }

    void SpawnObstacle()
    {
        if (obstacleTypes.Length == 0) return;

        int idx = Random.Range(0, obstacleTypes.Length);
        ObstacleData data = obstacleTypes[idx];
        if (data.prefab == null) return;

        float lane = lanes[Random.Range(0, lanes.Length)];
        Vector3 pos = new Vector3(lane, 0.5f, playerTr.position.z + spawnDistance);

        GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity);
        var obs = obj.GetComponent<Obstacle>();
        if (obs != null)
            obs.data = data;

        spawnedObjects.Add(obj);
    }

    void SpawnBonus()
    {
        if (bonusPrefab == null) return;

        int idx = Random.Range(0, bonusTypes.Length);
        BonusData bd = bonusTypes[idx];

        float lane = lanes[Random.Range(0, lanes.Length)];
        Vector3 pos = new Vector3(lane, 1f, playerTr.position.z + spawnDistance);

        GameObject obj = Instantiate(bonusPrefab, pos, Quaternion.identity);
        var pickup = obj.GetComponent<BonusPickup>();
        if (pickup != null)
            pickup.data = bd;

        spawnedObjects.Add(obj);
    }
}
