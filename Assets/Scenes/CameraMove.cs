using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RotationAxes { 
    MouseXAndY = 0, MouseX = 1, MouseY = 2 
}

public class CameraMove : MonoBehaviour
{
    private Vector3 dirVector3;
    private Vector3 rotaVector3;
    private float paramater = 0.1f;
    //x轴（水平）速度
    public float sensitivityX = 15F;
    //y轴（垂直）速度
    public float sensitivityY = 15F;
    
    //y轴（垂直）最小旋转值
    public float minimumY = -60F;
    //y轴（垂直）最大旋转值
    public float maximumY = 60F;
    //旋转轴
    public RotationAxes axes = RotationAxes.MouseXAndY;
    private float rotationY = 0F;
 
    void Update ()
    {
        if (axes == RotationAxes.MouseXAndY && Input.GetMouseButton(0))
        {
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
			
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
        else if (axes == RotationAxes.MouseX)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
        }
        else if (axes == RotationAxes.MouseY)
        {
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
            transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
        }
        
        dirVector3 =Vector3.zero;
 
        if (Input.GetKey(KeyCode.W))
        {
            if(Input.GetKey(KeyCode.LeftShift)) dirVector3.z = 3;
            else dirVector3.z = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.LeftShift)) dirVector3.z = -3;
            else dirVector3.z = -1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            if (Input.GetKey(KeyCode.LeftShift)) dirVector3.x = -3;
            else dirVector3.x = -1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.LeftShift)) dirVector3.x = 3;
            else dirVector3.x = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            if (Input.GetKey(KeyCode.LeftShift)) dirVector3.y = -3;
            else dirVector3.y = -1;
        }
        if (Input.GetKey(KeyCode.E))
        {
            if (Input.GetKey(KeyCode.LeftShift)) dirVector3.y = 3;
            else dirVector3.y = 1;
        }
        transform.Translate(dirVector3 * paramater,Space.Self);
    }
}
