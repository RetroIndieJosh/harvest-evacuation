using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanSphere : MonoBehaviour {
    [SerializeField]
    private float m_scanTimeSec = 1.0f;

    [SerializeField]
    private float m_scanArcDeg = 30.0f;

    private Unit m_curTarget = null;

    private void OnTriggerStay( Collider other ) {
        var unit = other.GetComponent<Unit>();
        if ( unit == null ) return;

        var targetDot = 1.0f - 2.0f * m_scanArcDeg / 360.0f;

        var vecToCollider = ( other.transform.position - transform.position ).normalized;
        var dot = Vector3.Dot( vecToCollider, transform.forward );
        if ( dot < targetDot ) return;

        if( m_curTarget != unit ) {
            var source = unit.GetComponent<AudioSource>();
            source.clip = HarvestManager.instance.ScanClip;
            source.Play();
        }
        m_curTarget = unit;

        //unit.GetComponentInChildren<SpriteRenderer>().color = Color.red;
        unit.CheckAlien();
    }
}
