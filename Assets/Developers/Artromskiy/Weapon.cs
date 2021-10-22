using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Базовый класс для всех вариантов оружия
/// </summary>
public abstract class Weapon: MonoBehaviour
{
    [SerializeField]
    /// <summary>
    /// Параметр максимальной точности, влияет на смещение направления выстрела
    /// </summary>
    protected float accuracy;
    [SerializeField]
    /// <summary>
    /// Используется как параметр нанесения урона персонажу
    /// </summary>
    protected float damage;
    [SerializeField]
    /// <summary>
    /// Используется как параметр для рэйкаста и т.д.
    /// </summary>
    protected float range;
    [SerializeField]
    /// <summary>
    /// Время ожидания перед следующим выстрелом
    /// </summary>
    protected float rate;
    [field: SerializeField]
    /// <summary>
    /// Используется персонажем, как параметр влияющий на конечную скорость движения
    /// </summary>
    public float weight{get; private set;}
    [SerializeField]
    /// <summary>
    /// Используется как параметр, изменяющий текущую точность
    /// </summary>
    protected float recoil;
    [SerializeField]
    /// <summary>
    /// Максимальный магазин орудия
    /// </summary>
    private int magazine;
    [SerializeField]
    /// <summary>
    /// Время проведения перезарядки
    /// </summary>
    protected float reloadTime;
    /// <summary>
    /// Текущее количество патронов в магазине.
    /// Используется для инициализации перезарядки.
    /// </summary>
    /// <value></value>
    protected int CurrentMagazine{get; private set;}
    /// <summary>
    /// Текущая точность, расчитываемая на основе времени,
    /// событий выстрелов и параметра отдачи
    /// </summary>
    /// <value></value>
    protected float CurrentAccuracy{get; private set;}
    [SerializeField]
    /// <summary>
    /// Используется, для описания логики стрельбы. Лучше представлять
    /// этот параметр, как нажатие спускового крючка орудия
    /// </summary>
    protected bool canShoot = false;
    /// <summary>
    /// Событие срабатывающее, когда зажат спусковой крючок, и время rate прошло
    /// </summary>
    public UnityEvent shootEvent;
    /// <summary>
    /// Событие срабатывающее при отсутствии патронов в магазине
    /// </summary>
    public UnityEvent reloadEvent;
    /// <summary>
    /// Стандартная инициализация. При перезаписывании
    /// сначала вызовите родительский метод, иначе
    /// закрытые слушающие методы будут утеряны
    /// </summary>
    protected virtual void Start()
    {
        canShoot = false;
        if(shootEvent==null)
        shootEvent = new UnityEvent();
        if(reloadEvent == null)
        reloadEvent = new UnityEvent();
        if(shootEvent != null)
        {
            shootEvent.AddListener(Shoot);
            shootEvent.AddListener(DoRecoil);
        }
        StartCoroutine(SpecifyEvents());
    }
    
    void Update()
    {
        SpecifyAccuracy();
    }
    /// <summary>
    /// Базовый метод для выстрела, определите в нём всю логику
    /// получения коллайдеров и вызовите метод Damage внутри.
    /// Этот метод будет автоматически вызываться через событие
    /// </summary>
    protected abstract void Shoot();
    /// <summary>
    /// Вызывайте при нажатии клавиши выстрела.
    /// Используйте, чтобы прописать логику изменения для переменной
    /// canShoot, или другой переменной со сходным применеием.
    /// </summary>
    public abstract void StartShooting();
    /// <summary>
    /// Вызывайте при отпускании клавиши выстрела.
    /// Используйте, чтобы прописать логику изменения для переменной
    /// canShoot, или другой переменной со сходным применением.
    /// </summary>
    public abstract void EndShooting();
    /// <summary>
    /// Основной метод, для нанесения урона, не следует переписывать, 
    /// передавайте в него все коллайдеры, которым нужно нанести урон.
    /// </summary>
    /// <param name="colliders"></param>
    protected void Damage(Collider[] colliders)
    {
        for(int i = 0; i < colliders.Length; i++)
        {
            if(colliders[i] != null)
            Debug.Log(colliders[i].name);
            // TODO
        }
    }

    /// <summary>
    /// Уменьшает текущую точность на параметр отдачи.
    /// </summary>
    protected void DoRecoil()
    {
        CurrentAccuracy-=recoil;
    }
    /// <summary>
    /// Устремляет параметр текущей точности к параметру
    /// максимальной точности на основании времени и силы отдачи.
    /// </summary>
    private void SpecifyAccuracy()
    {
        if(CurrentAccuracy == accuracy)
        {
            return;
        }
        if(CurrentAccuracy<accuracy)
        {
            CurrentAccuracy+=recoil*Time.deltaTime;
        }
        if(CurrentAccuracy>accuracy)
        {
            CurrentAccuracy = accuracy;
        }
    }

    /// <summary>
    /// Вызывает события выстрела и перезарядки.
    /// </summary>
    /// <returns></returns>
    IEnumerator SpecifyEvents()
    {
        while(true)
        {
            if(CurrentMagazine==0)
            {
                reloadEvent.Invoke();
                yield return new WaitForSeconds(reloadTime);
                Reload();
            }
            if(canShoot)
            {
                CurrentMagazine--;
                shootEvent.Invoke(); // Вызов привязанных событий
                if(CurrentMagazine!=0)
                {
                    yield return new WaitForSeconds(rate);   
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    protected void Reload()
    {
        CurrentMagazine = magazine;
    }
}