using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HarvestManager : MonoBehaviour {
    static public HarvestManager instance = null;

    public AudioClip AlienDieClip = null;
    public AudioClip HumanDieClip = null;
    public AudioClip AlienRevealClip = null;
    public AudioClip ScanClip = null;

    [SerializeField]
    private AudioSource m_instructions = null;

    [SerializeField]
    private int m_level = 0;

    [SerializeField]
    private SpriteRenderer m_levelDisplay = null;

    [SerializeField]
    private Rigidbody m_bloodDropPrefab = null;

    [SerializeField]
    private Rigidbody m_bloodSplatPrefab = null;

    [SerializeField]
    private Spawner m_spawner = null;

    [SerializeField]
    private Ship m_ship = null;

    [SerializeField]
    private int m_alienHealth = 0;

    [Header( "Sprites" )]

    [SerializeField]
    private SpriteAnimator[] m_alienAnimPrefabList = null;

    [SerializeField]
    private Sprite m_alienIcon = null;

    [SerializeField]
    private SpriteAnimator[] m_humanAnimPrefabList = null;

    [SerializeField]
    private Sprite[] m_humanIconList = null;

    [Header( "Layer Masks" )]

    [SerializeField]
    private LayerMask m_unitLayerMask = -1;

    [Header( "Alien Speed" )]

    [SerializeField]
    private float m_angryAlienSpeedMultBase = 1.5f;

    [SerializeField]
    private float m_angryAlienSpeedMultIncPerLvl = 0.1f;

    [Header( "Alien Nomming" )]

    [SerializeField]
    private float m_angryAlienNomTimeBaseSec = 1.0f;

    [SerializeField]
    private float m_angryAlienNomTimeDecPerLvl = 0.1f;

    [Header( "Alien Chance" )]

    [SerializeField]
    private float m_alienChanceBase = 50.0f;

    [SerializeField]
    private float m_alienChanceIncPerLevel = 5.0f;

    [SerializeField]
    private float m_alienChanceMax = 95.0f;

    [Header( "Speed Min/Max" )]

    [SerializeField]
    private float m_speedMinBase = 1.0f;

    [SerializeField]
    private float m_speedMinIncPerLevel = 0.5f;

    [SerializeField]
    private float m_speedMaxBase = 1.0f;

    [SerializeField]
    private float m_speedMaxIncPerLevel = 0.5f;

    [Header("Spawn Time")]

    [SerializeField, Tooltip("Spawn time for level 0")]
    private float m_spawnTimeBaseBase = 3.0f;

    [SerializeField, Tooltip("Change in spawn time (delta)")]
    private float m_spawnTimeBaseChangeAccel = 0.1f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float m_spawnTimeVariationPercent = 0.1f;

    [Header( "Ship Slots" )]

    [SerializeField]
    private int m_shipSlotsBase = 5;

    [SerializeField]
    private int m_shipSlotsAddPerLevel = 3;

    public int HumanCount = 0;

    public float AlienChance {
        get {
            if ( HumanCount >= ShipSlots ) return 100.0f;

            var chance = m_alienChanceBase + m_alienChanceIncPerLevel * m_level;
            return Mathf.Min( chance, m_alienChanceMax );
        }
    }

    public int AlienHealth {  get { return m_alienHealth; } }

    public Sprite AlienIcon {  get { return m_alienIcon; } }

    public float AlienNomTime {
        get { return m_angryAlienNomTimeBaseSec - m_angryAlienNomTimeDecPerLvl * m_level; }
    }

    public float AngryAlienSpeedMult {
        get { return m_angryAlienSpeedMultBase + m_angryAlienSpeedMultIncPerLvl * m_level; }
    }

    public Rigidbody BloodDropPrefab {  get { return m_bloodDropPrefab; } }
    public Rigidbody BloodSplatPrefab {  get { return m_bloodSplatPrefab; } }

    [Header("MIB Speed")]
    [SerializeField]
    private float m_mibSpeedMult = 1.2f;

    public float MibSpeed { get { return ( m_speedMaxBase + m_speedMaxIncPerLevel * m_level ) * m_mibSpeedMult; } }

    public int ShipSlots { get { return m_shipSlotsBase + m_shipSlotsAddPerLevel * m_level; } }

    public LayerMask UnitLayerMask {  get { return m_unitLayerMask; } }

    public float UnitSpeedRandom {
        get {
            var speedMin = m_speedMinBase + m_speedMinIncPerLevel * m_level;
            var speedMax = m_speedMaxBase + m_speedMaxIncPerLevel * m_level;
            return Random.Range( speedMin, speedMax );
        }
    }

    public SpriteAnimator AlienAnimator {
        get {
            var i = Random.Range( 0, m_alienAnimPrefabList.Length );
            return m_alienAnimPrefabList[i];
        }
    }

    public int HumanTypeIndexRandom {
        get {  return Random.Range( 0, m_humanAnimPrefabList.Length );}
    }

    public SpriteAnimator GetHumanAnimator(int a_index ) {
        return m_humanAnimPrefabList[a_index];
    }

    public Sprite GetHumanIcon(int a_index ) {
        return m_humanIconList[a_index];
    }

    public void EndLevel() {
        foreach( var unit in FindObjectsOfType<Unit>() )
            unit.CheckAlien();
        m_spawner.IsPaused = true;
    }

    public void NextLevel() {
        ++m_level;
        foreach ( var unit in FindObjectsOfType<Unit>() )
            Destroy( unit.gameObject );

        SetupLevel();
    }

    public void RestartLevel() {
        foreach ( var unit in FindObjectsOfType<Unit>() )
            Destroy( unit.gameObject );

        SetupLevel();
    }

    private void Awake() {
        if( instance != null ) {
            Destroy( gameObject );
            return;
        }
        instance = this;

        var mib = FindObjectOfType<Mib>();
        m_mibInitialPos = mib.transform.position;
        m_mibInitialRot = mib.transform.rotation;
    }

    private void Update() {
        //m_spawner.IsPaused = m_spawner.SpawnCount >= ShipSlots;
    }

    private void Start() {
        SetupLevel();
    }

    private Vector3 m_mibInitialPos = Vector3.zero;
    private Quaternion m_mibInitialRot = Quaternion.identity;

    private void SetupLevel() {
        var baseChange = 0.0f;
        for( int i = 0; i < m_level; ++i )
            baseChange += m_spawnTimeBaseChangeAccel;
        var baseTime = m_spawnTimeBaseBase - baseChange;
        m_spawner.SetSpawnTime( baseTime, baseTime * m_spawnTimeVariationPercent );

        Ship.instance.IsLaunched = false;
        var mib = FindObjectOfType<Mib>();
        mib.LeaveShip();
        mib.transform.position = m_mibInitialPos;
        mib.transform.rotation = m_mibInitialRot;
        mib.GetComponent<TankMover>().Speed = MibSpeed;

        m_spawner.ResetSpawner();
        m_ship.ResetShip();
        m_levelDisplay.size = Vector2.right * m_level + Vector2.up;

        Debug.LogFormat( "Level {0}, alien chance {1}%", m_level, AlienChance );

        var source = GetComponent<AudioSource>();
        if( !source.isPlaying) source.Play();

        //if ( m_level == 0 ) PlayInstructions();
    }

    public void PlayInstructions() {
        StartCoroutine( PlayInstructionsCoroutine() );
    }

    private IEnumerator PlayInstructionsCoroutine() {
        GetComponent<AudioSource>().Pause();
        InputManager.instance.IsPaused = true;
        m_spawner.IsPaused = true;

        m_instructions.Play();
        var timeElapsed = 0.0f;
        while( timeElapsed < m_instructions.clip.length ) {
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
            if ( Input.GetKeyDown(KeyCode.Space) ) break;
            if ( Input.GetKeyDown(KeyCode.Escape) ) Application.Quit();
        }

        m_instructions.Stop();
        InputManager.instance.IsPaused = false;
        m_spawner.IsPaused = false;

        GetComponent<AudioSource>().UnPause();
    }
}
