using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mib : MonoBehaviour {
    [SerializeField]
    private bool m_isInShip = false;

    [SerializeField]
    private SpriteRenderer[] m_rendererList = null;

    [SerializeField]
    private GameObject m_lockdownWall = null;

    [SerializeField]
    private SpriteRenderer m_iconRenderer = null;

    [SerializeField]
    private LineRenderer m_aimer = null;

    [SerializeField]
    private float m_aimerLength = 5.0f;

    [SerializeField]
    private Rect m_bounds = new Rect();

    private bool IsInShip {
        set {
            m_isInShip = value;
            foreach( var renderer in m_rendererList )
                renderer.enabled = !m_isInShip;
            m_lockdownWall.SetActive( m_isInShip );
            m_iconRenderer.enabled = m_isInShip;
            Ship.instance.HasMib = m_isInShip;
        }
    }

    public void LeaveShip() {
        if ( !m_isInShip || Ship.instance.IsLaunched ) return;
        IsInShip = false;
        transform.position = Ship.instance.MibLeaveShipPos;
        transform.forward = Vector3.right;
    }

    private void Awake() {
        m_rendererList = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        var center = new Vector3( m_bounds.center.x, transform.position.y, m_bounds.center.y );
        var size = new Vector3( m_bounds.size.x, 0.1f, m_bounds.size.y );
        Gizmos.DrawWireCube( center, size );
    }

    private void Update() {
        m_aimer.SetPosition( 0, m_aimer.transform.position );
        m_aimer.SetPosition( 1, m_aimer.transform.position + transform.forward * m_aimerLength );

        var x = Mathf.Clamp( transform.position.x, m_bounds.xMin, m_bounds.xMax );
        var z = Mathf.Clamp( transform.position.z, m_bounds.yMin, m_bounds.yMax );
        transform.position = new Vector3( x, 0.0f, z );
    }

    private void Start() {
        m_aimer.positionCount = 2;
        m_aimer.startWidth = 0.1f;
        m_aimer.endWidth = 0.01f;
        m_aimer.startColor = m_aimer.endColor = Color.red;
        m_aimer.material = new Material( Shader.Find( "Unlit/Texture" ) );
        m_aimer.material.color = Color.red;

        IsInShip = false;
    }

    private void OnTriggerEnter( Collider other ) {
        if ( m_isInShip ) return;

        var ship = other.GetComponent<Ship>();
        if ( ship == null || ship.IsLaunched ) return;

        IsInShip = true;
    }
}
