using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
  public Animator anim;
  protected Rigidbody2D rb;
  private SpriteRenderer sr;

  public float speed;
  private float currSpeed;
  public float debuffAmount;
  public float shotInterval;
  public float jumpForce;
  public float dmg;
  [Tooltip("0 < reduce speed < 1")]
  public float reduceSpeed;

  [HideInInspector]public bool isGrounded = true;
  [HideInInspector] public bool isControlled = false;

  protected int lastDir = 1;

  public GameObject basicProjPrefab;
  public float shotSpeed;
  public float shotLife;

  public Transform shotPoint;

  private bool leverActivate = false;
  public float levInteractCool = 1f;
  public float levInteractTimer = 0f;

  public LayerMask levLayer;
  public float levInterRange = 1f;
  public LayerMask itemLayer;

  public Transform respawnPoint1;
  public Transform respawnPoint2;

  private Dictionary<ItemType, bool> collectedItem = new Dictionary<ItemType, bool>();

  public ItemType myAssignedItemType;

  private bool isKnocback = false;

  private Coroutine parentCoroutine;

  void Awake()
  {
    anim = GetComponent<Animator>();
    rb = GetComponent<Rigidbody2D>();
    rb.freezeRotation = true;
    sr = GetComponentInChildren<SpriteRenderer>();
    currSpeed = speed;

    foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
    {
        collectedItem[type] = false;
    }
  }


  protected virtual void Update()
  {
    if (!isControlled) return;
    Move();
    KeyInputControl();

    if(levInteractTimer > 0)
    {
      levInteractTimer -= Time.deltaTime;
    }
  }

  protected virtual void Move() 
  {
    if (isKnocback) return;
    float x = Input.GetAxisRaw("Horizontal");
    float currSpeed = rb.linearVelocity.x;
    if (x != 0)
    {
      lastDir = (int)Mathf.Sign(x);
      sr.flipX = (lastDir == -1);
    }
   
    float targetSpeed = x * speed;

    if (!isGrounded)
    {
      if (Mathf.Sign(currSpeed) == Mathf.Sign(x) || x == 0)
      {
        rb.linearVelocity = new Vector2(currSpeed, rb.linearVelocity.y);
      }
      else if(Mathf.Sign(currSpeed) != Mathf.Sign(x) && x != 0)
      {
        float smoothX = Mathf.Lerp(currSpeed, targetSpeed, reduceSpeed);
        rb.linearVelocity = new Vector2(smoothX, rb.linearVelocity.y);
      }
      else if(x == 0)
      {
        float smoothX = Mathf.MoveTowards(currSpeed, targetSpeed, reduceSpeed);
        rb.linearVelocity = new Vector2(smoothX, rb.linearVelocity.y);
      }
    }
    else
      rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);

    anim.SetBool("isWalk", x != 0 && isGrounded);

    if(Input.GetKeyDown(KeyCode.Space) && isGrounded)
    {
      rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
      rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
      isGrounded = false;
      anim.SetBool("isJump", !isGrounded);
    }
  }

  public void RemoveItem(ItemType type)
  {
    if (collectedItem.ContainsKey(type))
    {
      collectedItem[type] = false;
    }
  }

  protected virtual void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Ground")|| collision.gameObject.CompareTag("Props"))
    {
      isGrounded = true;
      anim.SetBool("isJump", false);

      if(parentCoroutine != null)
      {
        StopCoroutine(parentCoroutine);
        parentCoroutine = null;
      }

      parentCoroutine =  StartCoroutine(SetParentDelay(collision.transform));
    }
        if (collision.gameObject.CompareTag("Portal")) { }
  }

  private void OnCollisionExit2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Ground"))
    {
      if(parentCoroutine != null)
      {
        StopCoroutine(parentCoroutine);
        parentCoroutine = null;
      }
      if(this != null && isActiveAndEnabled && gameObject.activeInHierarchy)
      {
        StartCoroutine(UnsetParentDelay());
      }
      else
      {
        if(Application.isPlaying)
          transform.parent = null;
      }
    }
  }
  protected virtual void OnTriggerEnter2D(Collider2D collision)
  {
    if(collision.gameObject.layer == LayerMask.NameToLayer("PlayerBlock"))
    {
      rb.linearVelocity = new Vector2(-1f, rb.linearVelocity.y);
    }
  }
  IEnumerator SetParentDelay(Transform newP)
  {
    yield return null;
    if(this != null && isActiveAndEnabled && gameObject.activeInHierarchy)
    {
      transform.parent = newP;
    }
  }

  IEnumerator UnsetParentDelay()
  {
    yield return null;
    if (this != null && isActiveAndEnabled && gameObject.activeInHierarchy && Application.isPlaying)
    {
      transform.parent = null;
    }
  }

  public void SetControlled(bool controlled)
  {
    isControlled = controlled;
    if (!controlled)
    {
      transform.parent = null;
    }
    gameObject.layer = LayerMask.NameToLayer(controlled ? "PlayerActive" : "PlayerInActive");
    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
  }

  // J : Basick Attack. K : Skill1. L : Skill2. O : Interact lever
  protected virtual void KeyInputControl()
  {
    if (Input.GetKeyDown(KeyCode.J))
    {
      BasicAtck();
    }
    if (Input.GetKeyDown(KeyCode.O)) 
    {
      if (levInteractTimer <= 0f)
      {
        bool didSomething = TryInteractLever() || TryItem();
        if (didSomething)
        {
          levInteractTimer = levInteractCool;
        }
        else
        {
          Debug.Log("Failed manipulate the lever!");
        }
      }
      else Debug.Log($"Lever Cooldown. Remain Time : {levInteractTimer}");
    }
  }

  private bool TryInteractLever()
  {
    Vector2 centre = transform.position + Vector3.up * 0.5f;
    Collider2D[] hits = Physics2D.OverlapCircleAll(centre, levInterRange, levLayer);
    foreach(var hit in hits)
    {
      if (hit.TryGetComponent(out LeverHandle lev))
      {
        lev.TryActivate();
        return true;

      }
      if(hit.TryGetComponent(out LeverHandleToggle toggleLev))
      {
        toggleLev.TryToggle();
        return true;
        
      }

      if(hit.TryGetComponent(out Generator generator))
      {
        generator.TryActivate(this);
        return true;
      }
    }
    return leverActivate;
  }

  bool TryItem()
  {
    Vector2 centre = transform.position + Vector3.up * 0.5f;
    Collider2D[] hits = Physics2D.OverlapCircleAll(centre, levInterRange, itemLayer);
    foreach (var hit in hits)
    {
      if(hit.TryGetComponent(out Item item))
      {
        if(item.itemType == myAssignedItemType)
        {
          CollectItem(item.itemType);
          Destroy(item.gameObject);
          Debug.Log($"{item.itemType} obtained");
        }
        else
        {
          Debug.Log("Type Difference! Can't obtain this item");
        }
        return true;
      }
    }
    return false;
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.yellow;
    Vector3 centre = transform.position + Vector3.up * 0.5f;
    Gizmos.DrawWireSphere(centre, levInterRange);
  }

  public void Respawn()
  {
    Debug.Log("Respawn!");
    //transform.position = respawnPoint1.position;

    float y = transform.position.y;
    float midPointY = 9f;

    if (y < midPointY && respawnPoint1 != null)
    {
      transform.position = respawnPoint1.position;
    }
    else if(respawnPoint2 != null)
    {
      transform.position = respawnPoint2.position;
    }

    rb.linearVelocity = Vector2.zero;
    if (anim != null)
    {
      anim.enabled = true;
      anim.Rebind();
      anim.Update(0f);
      SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
      if (sr != null) sr.enabled = true;

      anim.SetTrigger("respawn");
    }

    anim.SetBool("isJump", false);
    anim.SetBool("isWalk", false );
  }

  public void ApplyDebuff(float amount)
  {
    currSpeed -= amount;
    if (currSpeed < 0f) { currSpeed = 0.5f; }
    StartCoroutine(RemoveDebuff(amount, 1.5f));
  }
  protected virtual void BasicAtck()
  {
    anim.SetTrigger("shot");
  }

  protected virtual void Skill1() { }

  public void ApplyKnockback(Vector2 dir, float force)
  {
    if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && rb.simulated)
    {
      isKnocback = true;
      rb.linearVelocity = Vector2.zero;
      rb.AddForce(dir * force, ForceMode2D.Impulse);
      Debug.Log("[Knockback] Force applied!");
      StartCoroutine(ResetKnockback());
    }
  }

  IEnumerator ResetKnockback()
  {
    yield return new WaitForSeconds(0.3f);
    isKnocback = false;
  }

  IEnumerator RemoveDebuff(float amount, float delay)
  {
    yield return new WaitForSeconds(delay);
    currSpeed += amount;
  }
  public void CollectItem(ItemType type)
  {
    if (collectedItem.ContainsKey(type))
    {
      collectedItem[type] = true;
      ItemSlotUI.Instance?.UpdateUI(this);
    }
  }

  public bool HasItem(ItemType type)
  {
    return collectedItem.ContainsKey(type) && collectedItem[type];
  }

  public bool HasMyAssignedItem()
  {
    return HasItem(myAssignedItemType);
  }
}
