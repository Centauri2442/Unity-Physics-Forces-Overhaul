// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace Magnet.Physics
{
    public class SpawnerTest : MonoBehaviour
    {
        public static List<GameObject> allSpawned = new List<GameObject>();
        public static uint NumberToSpawn = 1;
        public GameObject Prefab;

        public static uint iteration = 0;

        [Button]
        public void Spawn()
        {
            for (int i = 0; i < NumberToSpawn; i++)
            {
                allSpawned.Add(Instantiate(Prefab, transform.position, transform.rotation));
                iteration++;
            }
        }
    }
}
