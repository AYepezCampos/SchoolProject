using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;

public class cameraController : MonoBehaviour
{

    [SerializeField] int sens;
    [SerializeField] int lockVertMin, lockVertMax;
    [SerializeField] bool invertY;

    float rotX;

    // Start is called before the first frame update
    void Start()
    {
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

    }

    // Update is called once per frame
    void Update()
    {

        //get input
        float mouseY = Input.GetAxis("Mouse Y") * sens * Time.deltaTime;
        float mouseX = Input.GetAxis("Mouse X") * sens * Time.deltaTime;

        // invert Y camera
        if (!invertY)
            rotX -= mouseY;
        else
            rotX += mouseX;

        //clamp the rotX on the x axis
        rotX = Mathf.Clamp(rotX, lockVertMin, lockVertMax);

        //rotate the player on the y axis and y axis
        transform.localRotation = Quaternion.Euler(rotX, 0, 0);
        transform.parent.Rotate(Vector3.up * mouseX);

    }
}
