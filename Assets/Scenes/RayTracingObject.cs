using System.Collections;
using System.Collections.Generic;
using Scenes;
using UnityEngine;
// ReSharper disable CheckNamespace

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingObject : MonoBehaviour
{
    private void OnEnable()
    {
        RayTracingMaster.RegisterObject(this);
    }
    private void OnDisable()
    {
        RayTracingMaster.UnregisterObject(this);
    }
}
