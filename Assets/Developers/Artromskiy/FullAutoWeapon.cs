using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс оружия имеющий автоматическую стрельбу
/// </summary>
public class FullAutoWeapon : Weapon
{
    [ContextMenu("Shoot")]
    public override void StartShooting()
    {
        canShoot = true;
    }

    [ContextMenu("EndShoot")]
    public override void EndShooting()
    {
        canShoot = false;
    }

    protected override void Shoot()
    {
        base.Damage(Ray());
    }

    public Collider[] Ray()
    {
        RaycastHit hit;
        Collider[] collider;
        Physics.Raycast(transform.position, transform.forward, out hit, range);
        if(hit.collider!=null)
        {
            collider = new Collider[1];
            collider[0] = hit.collider;
        }
        else
        {
            collider = new Collider[0];
        }
        return collider;
    }
}
