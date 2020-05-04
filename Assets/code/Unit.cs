using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Alien,
    Human
}

[RequireComponent( typeof( AiMover3d ) )]
[RequireComponent( typeof( Rigidbody ) )]
public class Unit : MonoBehaviour
{
    [SerializeField]
    private bool m_debug = false;

    [SerializeField]
    private UnitType m_unitType = UnitType.Human;

    [SerializeField]
    private bool m_stayHuman = false;

    [SerializeField]
    private SpriteAnimator m_animator = null;

    private AudioSource m_audioSource = null;
    private Unit m_targetUnitWeAreEating = null;

    public bool IsBeingEaten { get; set; }
    public bool IsInShip { get; set; }

    public Sprite IconSprite {  get { return m_iconSprite; } }
    public UnitType UnitType { get { return m_unitType; } }

    private float Speed {
        set {
            m_speed = value;
            m_body.velocity = Vector3.left * m_speed;
        }
    }

    private Rigidbody m_body = null;
    private int m_health = 1;
    private float m_speed = 1.0f;
    private bool m_revealedAsAlien = false;
    private AiMover3d m_mover = null;
    private bool m_speedUp = false;
    private bool m_isRevealing = false;
    private Sprite m_iconSprite = null;
    private bool m_isTargeted = false;

    public void CheckAlien() {
        if ( m_unitType != UnitType.Alien || m_revealedAsAlien || m_isRevealing )
            return;
        
        UpdateAnimation( true );
        StartCoroutine( RevealAlien() );
        m_audioSource.clip = HarvestManager.instance.AlienRevealClip;
        m_audioSource.Play();
    }

    private IEnumerator RevealAlien() {
        m_isRevealing = true;

        // wait a frame for animator to set up
        yield return new WaitForSeconds(0.1f);

        m_body.velocity = Vector3.zero;
        m_animator.SetAnimation( "spawn" );
        m_animator.SetAnimationNext( "move forward" );
        while ( m_animator.CurAnimation.Name == "spawn" ) yield return null;
        //m_animator.SetAnimation( "move forward" );
        m_revealedAsAlien = true;
        GetComponent<Collider>().isTrigger = true;

        m_isRevealing = false;
    }

    private bool m_isDying = false;

    public void Damage( Vector3 a_sourcePos, int a_damage = 1 ) {
        if ( m_isDying ) return;

        var dir = ( a_sourcePos - transform.position ).normalized;
        SpawnBloodFor( 0.1f, transform.position, dir );

        m_health -= a_damage;
    }

    private void Die() {
        if( m_debug) Debug.LogFormat( "{0} died", name );

        if ( m_unitType == UnitType.Alien )
            m_audioSource.clip = HarvestManager.instance.AlienDieClip;
        if ( m_unitType == UnitType.Human )
            m_audioSource.clip = HarvestManager.instance.HumanDieClip;
        m_audioSource.Play();

        m_body.velocity = Vector3.zero;
        m_animator.SetAnimation( "die" );
        m_animator.OnEnd.AddListener( delegate () {
            m_animator.OnEnd.RemoveAllListeners();
            Destroy( gameObject, 0.5f );
        } );

        Instantiate( HarvestManager.instance.BloodSplatPrefab, transform.position - Vector3.up * 0.5f, 
            Quaternion.identity );
    }

    private void Awake() {
        m_body = GetComponent<Rigidbody>();
        m_mover = GetComponent<AiMover3d>();
        m_audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter( Collision collision ) {
        if ( m_targetUnitWeAreEating != null ) return;

        var otherUnit = collision.gameObject.GetComponent<Unit>();
        if ( otherUnit == null ) return;

        if ( m_speedUp || otherUnit.m_speedUp || m_revealedAsAlien ) return;
        if( m_debug ) Debug.Log( "Go faster!" );
        if ( m_speed < otherUnit.m_speed ) {
            m_speedUp = true;
            Speed = otherUnit.m_speed * 1.1f;
        } else {
            otherUnit.m_speedUp = true;
            otherUnit.Speed = m_speed * 1.1f;
        }
    }

    private void OnDestroy() {
        if ( m_unitType == UnitType.Human ) --HarvestManager.instance.HumanCount;
        if ( m_targetUnitWeAreEating != null ) m_targetUnitWeAreEating.IsBeingEaten = false;
    }

    private void Start() {
        if ( m_unitType == UnitType.Human && !m_stayHuman
            && Random.Range( 0, 100 ) < HarvestManager.instance.AlienChance ) {

            m_unitType = UnitType.Alien;
        }

        if ( m_unitType == UnitType.Human ) ++HarvestManager.instance.HumanCount;

        name = m_unitType.ToString();
        if ( m_unitType == UnitType.Alien ) m_health = HarvestManager.instance.AlienHealth;
        else m_health = 1;

        m_speed = HarvestManager.instance.UnitSpeedRandom;
        Speed = m_speed;

        UpdateAnimation();

        if ( Ship.instance.HasMib && m_unitType == UnitType.Alien ) CheckAlien();
    }

    private void Update() {
        if ( m_isDying ) return;
        if ( m_health <= 0 ) {
            m_isDying = true;
            Die();
            return;
        }

        if ( m_targetUnitWeAreEating != null || IsBeingEaten ) m_body.velocity = Vector3.zero;
        else if ( m_mover.Target == null ) Speed = m_speed;

        if ( m_revealedAsAlien && m_mover.Target != null && IsValidFood( m_mover.Target ) == false ) {
            if( m_debug ) Debug.LogFormat( "Target {0} is no longer valid food", m_targetUnitWeAreEating );
            m_mover.Target = null;
        }

        HandleAngryAlien();
    }

    private void SpawnBlood( Vector3 a_origin, Vector3 a_direction ) {
        var blood = Instantiate( HarvestManager.instance.BloodDropPrefab, a_origin, Quaternion.identity );
        var bloodSpurtVar = 1.0f;
        var variation = Vector3.right * Random.Range( -bloodSpurtVar, bloodSpurtVar )
            + Vector3.forward * Random.Range( -bloodSpurtVar, bloodSpurtVar );
        var speed = 5.0f;
        blood.velocity = ( a_direction + variation ) * speed;
    }

    private void SpawnBloodFor( float a_seconds, Vector3 a_origin, Vector3 a_direction ) {
        StartCoroutine( SpawnBloodForCoroutine( a_seconds, a_origin, a_direction ) );
    }

    private IEnumerator SpawnBloodForCoroutine( float a_seconds, Vector3 a_origin, Vector3 a_direction ) {
        var timeElapsed = 0.0f;
        while ( timeElapsed < a_seconds ) {
            SpawnBlood( a_origin, a_direction );

            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator EatHuman( Unit a_target ) {
        if ( a_target == null || m_targetUnitWeAreEating != null || m_isDying ) yield break;

        m_targetUnitWeAreEating = a_target;
        a_target.IsBeingEaten = true;
        m_animator.SetAnimation( "eat" );

        a_target.m_body.velocity = Vector3.zero;
        m_body.velocity = Vector3.zero;
        m_mover.Target = null;

        var baseDirection = transform.position - a_target.transform.position;
        SpawnBloodFor( HarvestManager.instance.AlienNomTime, a_target.transform.position + Vector3.up * 0.75f, 
            baseDirection );
        var timeElapsed = 0.0f;
        while( a_target != null && timeElapsed < HarvestManager.instance.AlienNomTime ) {
            yield return null;
            timeElapsed += Time.deltaTime;
            if ( m_isDying ) yield break;
        }
        if ( a_target != null ) a_target.Die();

        m_targetUnitWeAreEating = null;
        m_animator.SetAnimation( "walk" );
    }

    private bool IsValidFood( GameObject a_possibleFood ) {
        if( m_debug ) Debug.LogFormat( "Check {0} valid food", a_possibleFood );

        // don't eat ourselves or go backward
        if ( a_possibleFood.gameObject == gameObject ) {
            if ( m_debug ) Debug.LogFormat( "{0} is NOT valid food because it's us", a_possibleFood );
            return false;
        }

        // don't eat other aliens or things other people are eating
        var unit = a_possibleFood.GetComponent<Unit>();
        if ( unit == null ) {
            if ( m_debug ) Debug.LogFormat( "{0} is NOT valid food because it's not a unit", a_possibleFood );
            return false;
        }

        if ( unit.UnitType == UnitType.Alien ) {
            if ( m_debug ) Debug.LogFormat( "{0} is NOT valid food because it's an alien", a_possibleFood );
            return false;
        }

        if ( unit.IsBeingEaten ) {
            if ( m_debug ) Debug.LogFormat( "{0} is NOT valid food because it's being eaten", a_possibleFood );
            return false;
        }

        if ( unit.m_health <= 0 ) {
            if ( m_debug ) Debug.LogFormat( "{0} is NOT valid food because it's dead", a_possibleFood );
            return false;
        }

        if ( unit.IsInShip ) {
            if ( m_debug ) Debug.LogFormat( "{0} is NOT valid food because it's in the ship", a_possibleFood );
            return false;
        }

        if ( m_debug ) Debug.LogFormat( "{0} is valid food", a_possibleFood );
        return true;
    }

    private void HandleAngryAlien() {
        if ( !m_revealedAsAlien || m_mover.Target != null || m_targetUnitWeAreEating != null ) return;

        m_body.velocity = Vector3.zero;

        var colliderList = Physics.OverlapSphere( transform.position, Mathf.Infinity,
            HarvestManager.instance.UnitLayerMask );
        GameObject closest = null;
        var closestDistance = Mathf.Infinity;
        foreach ( var c in colliderList ) {
            if ( !IsValidFood( c.gameObject ) ) continue;

            var distance = Vector3.Distance( transform.position, c.transform.position );
            if ( distance < closestDistance ) {
                closest = c.gameObject;
                closestDistance = distance;
            }
        }

        // no target? start toward ship again
        if ( closest == null ) {
            Speed = m_speed;
            return;
        }

        m_mover.Target = closest;
        m_mover.Mode = AiMoveMode.Seek;
        m_mover.TargetDistance = 1.0f;
        m_mover.Speed = m_speed * HarvestManager.instance.AngryAlienSpeedMult;
        m_body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;

        closest.GetComponent<Unit>().m_isTargeted = true;

        m_mover.OnArrived.AddListener( delegate () {
            if ( closest == null ) return;
            StartCoroutine( EatHuman( closest.GetComponent<Unit>() ) );
            m_mover.OnArrived.RemoveAllListeners();
        } );
    }

    private void UpdateAnimation( bool a_revealAlien = false ) {
        SpriteAnimator animatorPrefab = null;
        if ( a_revealAlien && m_unitType == UnitType.Alien )
            animatorPrefab = HarvestManager.instance.AlienAnimator;
        else {
            var index = HarvestManager.instance.HumanTypeIndexRandom;
            animatorPrefab = HarvestManager.instance.GetHumanAnimator( index );
            m_iconSprite = HarvestManager.instance.GetHumanIcon( index );
        }

        if ( m_unitType == UnitType.Alien ) m_iconSprite = HarvestManager.instance.AlienIcon;

        if ( m_animator != null ) Destroy( m_animator.gameObject );
        m_animator = Instantiate( animatorPrefab );
        m_animator.transform.SetParent( transform );
        m_animator.transform.localPosition = Vector3.zero;
    }
}
