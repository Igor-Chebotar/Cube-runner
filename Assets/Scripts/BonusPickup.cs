using UnityEngine;

public class BonusPickup : MonoBehaviour
{
    [HideInInspector] public BonusData data;

    void Start()
    {
        // на всякий случай ставим trigger, вдруг на префабе забыли
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        if (data != null)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = data.color;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (data == null)
        {
            Destroy(gameObject);
            return;
        }

        switch (data.type)
        {
            case BonusType.Heal:
                player.ApplyHeal((int)data.value);
                break;
            case BonusType.SpeedBoost:
                player.ApplySpeedBoost(data.value, data.duration);
                break;
            case BonusType.Invincibility:
                player.SetInvincible(data.duration);
                break;
        }

        // очки за подбор бонуса
        var sm = FindObjectOfType<ScoreManager>();
        if (sm != null) sm.AddBonusScore(50);

        Destroy(gameObject);
    }
}
