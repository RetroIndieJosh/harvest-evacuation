using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MibShooter : MonoBehaviour {
    [SerializeField]
    Rigidbody m_bulletPrefab = null;

    [SerializeField]
    private float m_bulletLifetime = 10.0f;

    [SerializeField]
    private float m_fireSpeed = 10.0f;

    [SerializeField]
    private AudioSource m_audioSource = null;

    private void Awake() {
        m_audioSource = GetComponent<AudioSource>();
    }

    public void Fire() {
        if ( Ship.instance.HasMib ) return;

        var bullet = Instantiate( m_bulletPrefab, transform.position, Quaternion.identity );
        Destroy( bullet.gameObject, m_bulletLifetime );

        bullet.velocity = transform.forward * m_fireSpeed;
        m_audioSource.Play();
    }
}
