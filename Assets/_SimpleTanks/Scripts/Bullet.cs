using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Tank Owner { get; private set; }

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
        SimpleGameManager.Instance.Bullets.Remove(this);
    }

    public void Init(Tank owner)
    {
        Owner = owner;
        var color = Owner.Team == Tank.TeamType.Blue ? Color.blue : Color.red;
        var r = GetComponent<MeshRenderer>();
        r.material.color = color;
    }
}
