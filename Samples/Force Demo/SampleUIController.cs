using System;
using Magnet.Physics.Force;
using TMPro;
using UnityEngine;

namespace Magnet.Physics
{
    public class SampleUIController : MonoBehaviour
    {
        public TextMeshProUGUI ForcePointCounter;
        public TextMeshProUGUI TotalSpawnCounter;
        public TextMeshProUGUI SpawnCount;


        private void Update()
        {
            ForcePointCounter.text = ForceFieldJobHandler.pointCounter.ToString();
            TotalSpawnCounter.text = $"Objects: {SpawnerTest.iteration}";
        }

        public void ChangeSpawnCount(float delta)
        {
            SpawnerTest.NumberToSpawn = (uint)Mathf.Clamp(SpawnerTest.NumberToSpawn + delta, 1, 50);

            SpawnCount.text = SpawnerTest.NumberToSpawn.ToString();
        }

        public void DestroyAllObjects()
        {
            foreach (var item in SpawnerTest.allSpawned)
            {
                Destroy(item);
            }

            SpawnerTest.iteration = 0;
        }
    }
}
