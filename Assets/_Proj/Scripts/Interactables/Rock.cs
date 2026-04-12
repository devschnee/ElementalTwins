using UnityEngine;

public class Rock : MonoBehaviour
{
  private Vector2 originalPos;
  public float resetThresholdY = 5f;

  private Rigidbody2D rb;
  public float bounceForce = 80f;
  void Start()
  {
    originalPos = transform.position;
    rb = GetComponent<Rigidbody2D>();
  }

  void Update()
  {
    float yDist = Mathf.Abs(transform.position.y - originalPos.y);

    if (yDist > resetThresholdY)
    {
      InstantReset();
    }
  }

  void InstantReset()
  {
    // eliminate physics effect
    rb.linearVelocity = Vector2.zero;
    rb.angularVelocity = 0f;

    transform.position = originalPos;

    Debug.Log("Rock Reset!");
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Wall"))
    {
      Vector2 currDir = rb.linearVelocity.normalized;
      if (currDir == Vector2.zero)
      {
        currDir = -collision.contacts[0].normal;
      }

      Vector2 oppDir = -currDir;
        rb.AddForce(oppDir * bounceForce, ForceMode2D.Impulse);

    }
  }
}
