using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {
    private void OnTriggerEnter( Collider other ) {
        var unit = other.GetComponent<Unit>();
        if ( unit == null ) return;

        unit.Damage( transform.position );
        Destroy( gameObject );
        if ( unit.UnitType == UnitType.Alien ) {
            unit.CheckAlien();
        } else {
            // penalty for killing people
        }
    }

    private void OnBecameInvisible() {
        Destroy( gameObject );
    }
}
