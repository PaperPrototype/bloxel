using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Bloxel;

public class Player : MonoBehaviour
{
    public VoxelClient voxelClient;
    public VoxelStore voxelStore;
    public float MetersPerSecond = 16;
    public float DegreesPerSecond = 16;
    public Camera child;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float upDown = Input.GetAxis("UpDown");

        float mouseY = Input.GetAxis("Mouse Y");
        float mouseX = Input.GetAxis("Mouse X");

        transform.position += child.transform.right * horizontal * 100 * Time.smoothDeltaTime;
        transform.position += child.transform.up * upDown * 100 * Time.smoothDeltaTime;
        transform.position += child.transform.forward * vertical * 100 * Time.smoothDeltaTime;

        child.transform.Rotate(new Vector3(mouseY * -1, 0, 0));
        transform.transform.Rotate(new Vector3(0, mouseX, 0));

        if (Input.GetMouseButtonDown(0))
        {
            if (voxelStore.RaycastRemoveVoxel(child))
            {
                Debug.Log("Voxel removed!");
            } else
            {
                Debug.Log("Voxel not removed!");
            }
        }
    }
}