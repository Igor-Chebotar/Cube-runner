using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [HideInInspector] public ObstacleData data;
    bool alreadyHit = false;

    void OnCollisionEnter(Collision col)
    {
        if (alreadyHit) return;
        if (!col.gameObject.CompareTag("Player")) return;

        alreadyHit = true;
        var player = col.gameObject.GetComponent<PlayerController>();
        if (player != null && data != null)
            player.TakeDamage(data.damage);
    }

    void Update()
    {
        // если у препятствия задана своя скорость — двигаем навстречу
        if (data != null && data.spd > 0)
        {
            transform.Translate(Vector3.back * data.spd * Time.deltaTime);
        }
    }
}
