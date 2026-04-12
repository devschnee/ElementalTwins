using UnityEngine;

public class Projectiles : MonoBehaviour
{
  public float dmg = 20f;
  public float speed = 15f;
  public float lifeTime = 2f;

  private Rigidbody2D rb;

  protected virtual void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    Destroy(gameObject, lifeTime);
  }
  public void InitProj(float duration)
  {
    lifeTime = duration;
    Destroy(gameObject, lifeTime);
  }
  public void SetDirection(Vector2 direction)
  {
    if (rb != null)
    {
      rb.linearVelocity = direction.normalized * speed;
    }
  }

  protected virtual void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("EnemyHazard") || collision.gameObject.CompareTag("EnemyAtck"))
    {
      Destroy(gameObject);
    }
    else if (collision.gameObject.CompareTag("Props") || collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Ground"))
    {
      Destroy(gameObject);
    }
  }
}
