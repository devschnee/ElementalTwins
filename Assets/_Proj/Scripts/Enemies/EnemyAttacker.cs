using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class EnemyAttacker : Enemy
{
  public float atckRange = 5f;
  public float atckInterval = 0.2f;

  [Header("HP Settings")]
  public float maxHp = 100f;
  private float currHp;

  [Header("Projectile Settings")]
  public GameObject projPrefab;
  public Transform shotPoint;
  public float projSpeed;
  public float debuffPlayerSpeed = 1f;
  public float knockbackForce = 0.5f;
  public float projLifetime = 2f;

  [Header("Item Drop Settings")]
  public GameObject item1;
  public GameObject item2;

  private float atckTimer;

  protected override void Start()
  {
    base.Start();
    currHp = maxHp;
    atckTimer = 0;
    ChangeState(new AttackerIdle(this));
  }
  protected override void Update()
  {
    base.Update();
  }
  protected override void FixedUpdate()
  {
    base.FixedUpdate();
    if (PlayerManager.Instance != null && PlayerManager.Instance.activePlayerTrans != null)
    {
      this.playerTarget = PlayerManager.Instance.activePlayerTrans;
    }
    else
    {
      this.playerTarget = null;
    }
    if(atckTimer > 0)
    {
      atckTimer -= Time.deltaTime;
    }
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("Projectiles") || collision.gameObject.layer == LayerMask.NameToLayer("Skills"))
    {
      Projectiles proj = collision.gameObject.GetComponent<Projectiles>();
      if (proj != null)
      {
        TakeDmg(proj.dmg);
        Destroy(collision.gameObject);
      }
    }
  }

  public void TakeDmg(float dmg)
  {
    currHp -= dmg;
    Debug.Log("Attacker current HP : " +  currHp);
    if (currHp <= 0)
    {
      anim.SetTrigger("die");
      Invoke(nameof(Die), 1f);
    }
  }

  private void Die()
  {
    Debug.Log("Attacker Died");
    Destroy(gameObject);
  }
  private void DropItem()
  {
    GameObject itemDrop = null;
    if(Random.Range(0, 1f) < 0.5f)
    {
      itemDrop = item1;
    }
    else {
      itemDrop = item2;
    }

    if (itemDrop != null)
    {
      Instantiate(itemDrop, transform.position, Quaternion.identity);
    }
  }

  // ==================================================
  // Define a nested FSM state class insdie
  // ==================================================

  private class AttackerIdle : EnemyState
  {

    public AttackerIdle(Enemy enemy) : base(enemy) { }
    public override void Enter()
    {
      print("idle상태 진입");
      if (enemy.playerTarget != null)
      {
        //TODO:idle애니메이션 재생
        
      }
    }

    public override void Execute()
    {
      //TODO:idle애니메이션 재생
      if (enemy.playerTarget != null)
      {
        float distToPlayer = Vector2.Distance(enemy.playerTarget.position, enemy.transform.position);
        EnemyAttacker attacker = (EnemyAttacker)enemy;

        Vector3 targetDir = enemy.playerTarget.position - enemy.transform.position;
        if (targetDir.x > 0)
        {
          enemy.transform.localScale = new Vector3(Mathf.Abs(enemy.transform.localScale.x), enemy.transform.localScale.y, enemy.transform.localScale.z);
        }
        else if (targetDir.x < 0)
        {
          enemy.transform.localScale = new Vector3(-Mathf.Abs(enemy.transform.localScale.x), enemy.transform.localScale.y, enemy.transform.localScale.z);
        }

        Vector2 diff = enemy.playerTarget.position - enemy.transform.position;
        float distX = Mathf.Abs(diff.x);
        float distY = Mathf.Abs(diff.y);

        if(distX < enemy.detectRangeX && distY < enemy.detectRangeY)
        {
          enemy.ChangeState(new AttackerAttack(attacker));
        }
      }
    }

    public override void Exit()
    {
      //TODO: idle애니메이션 중지
    }
  }

  private class AttackerAttack : EnemyState
  {
    public AttackerAttack(Enemy enemy) : base(enemy) { }

    public override void Enter() {
      EnemyAttacker attacker = (EnemyAttacker)enemy;
      attacker.anim.SetBool("isShot", true);
    }

    public override void Execute()
    {
      Debug.Log($"[FSM Execute] isFrozen: {enemy.isFrozen}");
      if (enemy.isFrozen) return;
      EnemyAttacker attacker = (EnemyAttacker)enemy;
      if (attacker.playerTarget == null || Vector2.Distance(attacker.playerTarget.position, attacker.transform.position) > attacker.atckRange)
      {
        print("Change State");
        attacker.ChangeState(new AttackerIdle(attacker));
        return;
      }

      Vector3 targetDir = attacker.playerTarget.position - attacker.transform.position;
      if(targetDir.x > 0)
      {
        attacker.transform.localScale = new Vector3(Mathf.Abs(attacker.transform.localScale.x), attacker.transform.localScale.y, attacker.transform.localScale.z);
      }
      else if (targetDir.x < 0)
      {
        attacker.transform.localScale = new Vector3(-Mathf.Abs(attacker.transform.localScale.x), attacker.transform.localScale.y, attacker.transform.localScale.z);
      }
      if (attacker.atckTimer <= 0)
      {
        attacker.ShootProjectile();
        attacker.atckTimer = attacker.atckInterval;
      }
    }

    public override void Exit()
    {
      EnemyAttacker attacker = (EnemyAttacker)enemy;
      attacker.anim.SetBool("isShot", false);
    }
  }

  private void ShootProjectile()
  {
    if (isFrozen) return;
    if (projPrefab == null || shotPoint == null) return;

    GameObject projGO = Instantiate(projPrefab, shotPoint.position, Quaternion.identity);
    Projectiles proj = projGO.GetComponent<Projectiles>();
    if(proj != null)
    {
      proj.InitProj(projLifetime);
    }

    foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
    {
      Collider2D projCol = projGO.GetComponent<Collider2D>();
      if (projCol != null && col != null)
      {
        Physics2D.IgnoreCollision(projCol, col);
      }
    }

    float xDir = Mathf.Sign(playerTarget.position.x - shotPoint.position.x);
    Vector2 dir = new Vector2(xDir, 0f);

    SpriteRenderer projSr = projGO.GetComponentInChildren<SpriteRenderer>();
    if (projSr != null)
    {
      projSr.flipX = (xDir < 0f);
    }
    Rigidbody2D projRb = projGO.GetComponent<Rigidbody2D>();
    if (projRb == null)
    {
      Destroy(projGO);
      return;
    }
    projRb.linearVelocity = dir * projSpeed;

    DebuffProjectile debuffProj = projGO.GetComponent<DebuffProjectile>();
    if (debuffProj == null)
    {
      debuffProj = projGO.AddComponent<DebuffProjectile>();
    }
    debuffProj.moveSpeedDebuff = debuffPlayerSpeed;
    debuffProj.knockbackForce = knockbackForce;
  }
}
