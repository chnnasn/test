using UnityEngine;

public interface IDamage
{
    public void TakeDamage(float damage);
    public void TakeDamage(float damage, Vector3 hitPoint);
}
