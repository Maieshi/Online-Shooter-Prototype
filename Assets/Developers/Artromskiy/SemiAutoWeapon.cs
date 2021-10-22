using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс оружия имеющий полуавтоматическую стрельбу
/// </summary>
public class SemiAutoWeapon: Weapon
{
    private bool shooted = false;
    [ContextMenu("Shoot")]
    public override void StartShooting()
    {
        if(!shooted)
        {
            canShoot = true;
        }
    }

    [ContextMenu("EndShoot")]
    public override void EndShooting()
    {
        shooted = false;
        canShoot = false;
    }

    protected override void Shoot()
    {
        base.Damage(Ray());
    }

    public Collider[] Ray()
    {
        Collider[] collider;
        Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range);
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

    private void AfterShoot()
    {
        canShoot = false;
        shooted = true;
    }

    protected override void Start()
    {
        base.Start();
        if(shootEvent!=null)
        {
            shootEvent.AddListener(AfterShoot);
        }
        shooted = false;
    }
}