using System.Collections;
using UnityEngine;

public class PControllerElec : Player
{
  private bool isDashing = false;
  public LayerMask dashColMask;
  

  public SkillData dashData;
  private float dashCoolTimer = 0f;
  protected override void Update()
  {
    base.Update();
    if (dashCoolTimer > 0f) { dashCoolTimer -= Time.deltaTime; }
  }
  protected override void Move()
  {
    base.Move();
  }

  protected override void KeyInputControl()
  {
    base.KeyInputControl();
    if (Input.GetKeyDown(KeyCode.K) && dashCoolTimer <= 0f)
    {
      Skill1();
      dashCoolTimer = dashData.cooldown;
    }
  }

  protected override void OnCollisionEnter2D(Collision2D collision)
  {
    base.OnCollisionEnter2D(collision);
    
  }
  protected override void BasicAtck()
  {
    base.BasicAtck();
    GameObject proj = Instantiate(basicProjPrefab, shotPoint.position, Quaternion.identity);

    ElecProjectile ep = proj.GetComponent<ElecProjectile>();
    
    ep.speed = shotSpeed;
    ep.lifeTime = shotLife;

    Vector2 dir = lastDir > 0 ? Vector2.right : Vector2.left;
    if (dir == Vector2.left && ep.srProjE != null)
    {
      ep.srProjE.flipX = true;
    }
    else ep.srProjE.flipX = false;
    ep.SetDirection(dir);
  }

  protected override void Skill1()
  {
    if (dashCoolTimer > 0f || isDashing) return;
    StartCoroutine(Dash());
    dashCoolTimer = dashData.cooldown;
    SkillSlotUI.Instance.ElecCoolDown(dashData.cooldown);
  }

  IEnumerator Dash()
  {
    isDashing = true;
    anim.SetBool("isK", true);

    float dashDist = dashData.range;
    float dashDuration = dashData.duration;
    int dashDmg = dashData.dmgMultiplier;

    Vector2 dir = new Vector2(lastDir, 0);
    float moved = 0f;
    float speed = dashDist / dashDuration;

    while(moved < dashDist)
    {
      float step = speed * Time.deltaTime;
      Vector2 start = rb.position;
      Vector2 next = start + dir * step;

      RaycastHit2D hit = Physics2D.Raycast(start, dir, step, dashColMask);
      if(hit.collider != null)
      {
        if(hit.collider.CompareTag("EnemyAtck"))
        {
          EnemyAttacker e = hit.collider.GetComponent<EnemyAttacker>();
          if(e != null)
          {
            e.TakeDmg(dmg * dashDmg);
          }
        }
        break;
      }
      rb.MovePosition(next);
      moved += step;
      yield return null;
    }
    rb.linearVelocity = Vector2.zero;
    anim.SetBool("isK", false);
    isDashing = false;
  }
}
