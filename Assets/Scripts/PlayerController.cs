using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float speed = 8f;
    [SerializeField] float laneDistance = 3f;
    public float jumpForce = 7f;
    [SerializeField] int maxHealth = 100;
    int _health;

    [SerializeField] float acceleration = 0.1f;
    float currentSpeed;

    Rigidbody rb;
    int jumpCount = 0;
    [SerializeField] int maxJumps = 2;

    bool isGrounded;
    [SerializeField] float groundCheckDist = 0.6f;
    [SerializeField] float groundCheckRadius = 0.3f;
    [SerializeField] LayerMask groundLayer;

    bool invincible = false;
    Renderer rend;
    Color originalColor;

    float speedBoostTimer = 0f;
    float bonusSpeedMult = 1f;

    // аниматор (может быть null если не назначен)
    Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        anim = GetComponent<Animator>();

        if (rend != null)
            originalColor = rend.material.color;
        _health = maxHealth;
        currentSpeed = speed;
    }

    void Update()
    {
        // ускорение со временем
        currentSpeed += acceleration * Time.deltaTime;
        float finalSpeed = currentSpeed * bonusSpeedMult;
        transform.Translate(Vector3.forward * finalSpeed * Time.deltaTime);

        // лево/право
        float h = Input.GetAxis("Horizontal");
        transform.Translate(Vector3.right * h * laneDistance * Time.deltaTime);

        // проверка земли
        isGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * groundCheckDist,
            groundCheckRadius, groundLayer);

        if (isGrounded && rb.velocity.y <= 0.1f)
            jumpCount = 0;

        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            jumpCount++;
        }

        // таймер спидбуста
        if (speedBoostTimer > 0)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0)
            {
                bonusSpeedMult = 1f;
                ResetColor();
            }
        }

        // обновляем параметры аниматора
        if (anim != null)
        {
            anim.SetBool("IsGrounded", isGrounded);
            anim.SetFloat("Speed", finalSpeed);
            anim.SetBool("Invincible", invincible);
            anim.SetBool("SpeedBoosted", speedBoostTimer > 0);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (invincible) return;

        _health -= dmg;

        if (anim != null)
            anim.SetTrigger("Hit");

        StartCoroutine(FlashRed());

        if (_health <= 0)
        {
            _health = 0;
            Die();
        }
    }

    IEnumerator FlashRed()
    {
        if (rend == null) yield break;
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        rend.material.color = Color.red * 0.7f;
        yield return new WaitForSeconds(0.15f);
        // возвращаем цвет только если нет другого эффекта
        if (!invincible && speedBoostTimer <= 0)
            rend.material.color = originalColor;
    }

    public void ApplyHeal(int amount)
    {
        _health += amount;
        if (_health > maxHealth) _health = maxHealth;

        if (anim != null)
            anim.SetTrigger("Bonus");

        StartCoroutine(FlashGreen());
    }

    IEnumerator FlashGreen()
    {
        if (rend == null) yield break;
        rend.material.color = Color.green;
        yield return new WaitForSeconds(0.3f);
        if (!invincible && speedBoostTimer <= 0)
            rend.material.color = originalColor;
    }

    public void ApplySpeedBoost(float multiplier, float dur)
    {
        bonusSpeedMult = multiplier;
        speedBoostTimer = dur;

        if (anim != null)
            anim.SetTrigger("Bonus");

        if (rend != null)
            rend.material.color = Color.yellow;
    }

    public void SetInvincible(float dur)
    {
        if (anim != null)
            anim.SetTrigger("Bonus");

        StartCoroutine(InvincibleRoutine(dur));
    }

    IEnumerator InvincibleRoutine(float dur)
    {
        invincible = true;
        if (rend != null)
            rend.material.color = Color.cyan;

        yield return new WaitForSeconds(dur);

        invincible = false;
        if (speedBoostTimer <= 0)
            ResetColor();
    }

    void ResetColor()
    {
        if (rend != null && !invincible)
            rend.material.color = originalColor;
    }

    void Die()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnPlayerDied();
        else
            Debug.LogWarning("GameManager не найден, рестарт невозможен");
    }

    public int GetHealth() { return _health; }
    public int GetMaxHealth() { return maxHealth; }
    public float GetCurrentSpeed() { return currentSpeed; }
}
