using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IHitBox
{
    IEnumerator Hit(Attack other);
    IEnumerator HitDelay(float damage);
    void Damage(float damage, bool isCritical, Vector2 hitPos);
    IEnumerator Dead();
    void DebuffRemove();

    IEnumerator PoisonDotHit(float tickDamage, float duration);
    IEnumerator BleedDotHit(float tickDamage, float duration);
    IEnumerator Knockback(Attack attacker, float knockbackForce);
    IEnumerator FlatDebuff(float flatTime);
    IEnumerator SlowDebuff(float slowDuration);
    IEnumerator ShockDebuff(float shockDuration);
}
