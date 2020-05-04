using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    [SerializeField]
    GameObject m_originObject = null;

    [SerializeField]
    private float m_rotateSpeed = 5.0f;

    [SerializeField]
    private Vector3 m_originRadius = Vector3.back;

    private void Start() {
        var parent = new GameObject();
        parent.transform.parent = transform.parent;
        transform.parent = parent.transform;
        transform.localPosition = m_originRadius;
    }

    void Update () {
        if ( m_originObject == null ) return;

        transform.parent.transform.position = m_originObject.transform.position;
        transform.parent.rotation = Quaternion.Euler( 0.0f, m_originObject.transform.rotation.eulerAngles.y, 0.0f );
        //transform.LookAt( m_originObject.transform );

        if ( Input.GetKeyDown( KeyCode.A ) )
            transform.parent.Rotate( Vector3.up, m_rotateSpeed );
            //transform.RotateAround( m_originObject.transform.position, Vector3.up, m_rotateSpeed );
        else if ( Input.GetKeyDown( KeyCode.D ) )
            transform.parent.Rotate( Vector3.up, -m_rotateSpeed );
            //transform.RotateAround( m_originObject.transform.position, Vector3.up, -m_rotateSpeed );
	}
}
