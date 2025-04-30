// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using Unity.Mathematics;
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// We gotta turn collider data into blittable types to feed into Unity Jobs 
    /// </summary>

    #region Conversion

    public static class JobColliderData
    {
        public static SphereColliderData ConvertSphere(SphereCollider sphere)
        {
            return new SphereColliderData(
                pos : sphere.transform.position, 
                r : sphere.radius
                );
        }

        public static BoxColliderData ConvertBox(BoxCollider box)
        {
            return new BoxColliderData(
                pos : box.transform.position,
                size : box.size,
                rot : box.transform.rotation
                );
        }

        public static CapsuleColliderData ConvertCapsule(CapsuleCollider capsule)
        {
            return new CapsuleColliderData(
                pos : capsule.transform.position,
                height : capsule.height,
                rad : capsule.radius,
                dir : capsule.direction,
                rot : capsule.transform.rotation
                );
        }
    }
    
    #endregion

    #region Structs

    public struct SphereColliderData
    {
        public float3 position;
        public float radius;

        public SphereColliderData(Vector3 pos, float r)
        {
            position = pos;
            radius = r;
        }
    }

    public struct BoxColliderData
    {
        public float3 position;
        public float3 extents;
        public float3x3 rotation;
        
        public BoxColliderData(Vector3 pos, Vector3 size, Quaternion rot)
        {
            position = pos;
            extents = size * 0.5f;
            rotation = new float3x3(rot);
        }
    }
    
    public struct CapsuleColliderData
    {
        public float3 position;
        public float height;
        public float radius;
        public int direction; // 0 = x | 1 = y | 2 = z
        public float3x3 rotation;

        public CapsuleColliderData(Vector3 pos, float height, float rad, int dir, Quaternion rot)
        {
            position = pos;
            this.height = height;
            radius = rad;
            direction = dir;
            rotation = new float3x3(rot);
        }
    }

    #endregion
}
