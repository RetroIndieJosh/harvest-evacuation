using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent( typeof( Rigidbody ) )]
public class Ship : MonoBehaviour
{
    static public Ship instance = null;

    [SerializeField]
    private GameObject m_mibLeaveShipMarker = null;

    public Vector3 MibLeaveShipPos { get { return m_mibLeaveShipMarker.transform.position; } }

    [Header( "Occupant Markers" )]

    [SerializeField]
    private GameObject m_occupantMarkerPrefab = null;

    [SerializeField]
    private float m_occupantMarkerRadius = 0.0f;

    [SerializeField]
    private float m_occupantMarkerY = 0.0f;

    [SerializeField]
    private float m_occupantMarkerAngleOffset = -15.0f;

    [SerializeField]
    private float m_occupantMarkerAngleTotal = 60.0f;

    [Header( "Launch" )]

    [SerializeField]
    private float m_launchMaxY = 0.1f;

    [SerializeField]
    private Vector3 m_launchAccel = Vector3.one * 0.1f;

    [Header( "Result" )]

    [SerializeField]
    private SpriteAnimator m_victoryPrefab = null;

    [SerializeField]
    private SpriteAnimator m_losePrefab = null;

    public bool HasMib { get; set; }

    private Vector3 m_initialPos = Vector3.zero;

    public bool IsLaunched = false;
    private List<Unit> m_occupantList = new List<Unit>();
    private List<SpriteRenderer> m_occupantMarkerRendererList = new List<SpriteRenderer>();

    Rigidbody m_body;

    private int OccupantsMax { get { return HarvestManager.instance.ShipSlots; } }

    public void ResetShip() {
        m_victoryPrefab.gameObject.SetActive( false );
        m_losePrefab.gameObject.SetActive( false );

        m_body.velocity = Vector3.zero;
        transform.position = m_initialPos;

        m_occupantList.Clear();

        IsLaunched = false;
        InitOccupantsDisplay();
    }

    private void Awake() {
        if( instance != null ) {
            Destroy( gameObject );
            return;
        }
        instance = this;

        m_body = GetComponent<Rigidbody>();
        m_initialPos = transform.position;
    }

    private void Start() {
        ResetShip();
    }

    private void OnTriggerEnter( Collider other ) {
        if ( IsLaunched ) return;

        var unit = other.GetComponent<Unit>();
        if ( unit == null ) return;

        unit.gameObject.SetActive( false );

        if ( unit.UnitType == UnitType.Alien ) {
            if ( m_occupantList.Count >= OccupantsMax )
                m_occupantList.RemoveAt( 0 );
            StartCoroutine( LaunchCoroutine( false ) );
        }

        m_occupantList.Add( unit );
        unit.IsInShip = true;

        UpdateOccupantsDisplay();
    }

    private void Update() {
        UpdateOccupantsDisplay();
        if ( !IsLaunched && m_occupantList.Count >= OccupantsMax && HasMib)
            StartCoroutine( LaunchCoroutine( true ) );
    }

    private void InitOccupantsDisplay() {
        foreach ( var occupantMarker in m_occupantMarkerRendererList )
            Destroy( occupantMarker, 0.1f );
        m_occupantMarkerRendererList.Clear();

        var angleDiff = m_occupantMarkerAngleTotal / OccupantsMax;
        var angle = -angleDiff * OccupantsMax / 2 + m_occupantMarkerAngleOffset;
        for ( int i = 0; i < OccupantsMax; ++i ) {
            var marker = Instantiate( m_occupantMarkerPrefab );
            marker.GetComponent<SpriteRenderer>().color = Color.white;
            marker.transform.SetParent( transform );

            var rotation = Quaternion.Euler( 0.0f, angle, 0.0f );
            var pos = Vector3.right * m_occupantMarkerRadius + Vector3.up * m_occupantMarkerY;
            marker.transform.localPosition = rotation * pos;

            m_occupantMarkerRendererList.Add( marker.GetComponent<SpriteRenderer>() );

            angle += angleDiff;
        }
    }

    private IEnumerator LaunchCoroutine( bool a_win ) {
        IsLaunched = true;
        GetComponent<AudioSource>().Play();

        HarvestManager.instance.EndLevel();

        var timeElapsed = 0.0f;
        while ( transform.position.y < m_launchMaxY ) {
            m_body.velocity += m_launchAccel;
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if ( a_win ) m_victoryPrefab.gameObject.SetActive( true );
        else m_losePrefab.gameObject.SetActive( true );

        yield return new WaitForSeconds( 6.0f );

        if( a_win ) HarvestManager.instance.NextLevel();
        else HarvestManager.instance.RestartLevel();
    }

    private void UpdateOccupantsDisplay() {
        for ( int i = 0; i < m_occupantMarkerRendererList.Count; ++i ) {
            var color = Color.white;
            if ( i < m_occupantList.Count ) {
                m_occupantMarkerRendererList[i].sprite = m_occupantList[i].IconSprite;
                if ( m_occupantList[i].IsBeingEaten ) m_occupantMarkerRendererList[i].color = Color.red;
            } else m_occupantMarkerRendererList[i].sprite = m_occupantMarkerPrefab.GetComponent<SpriteRenderer>().sprite;
        }
    }
}