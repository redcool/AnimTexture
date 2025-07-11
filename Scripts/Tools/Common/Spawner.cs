﻿namespace AnimTexture
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Spawner : MonoBehaviour
    {
        public GameObject[] prefabs;
        public int count = 500;
        public float radius = 10;
        public float y = 20;

        public float animSampleRate = 30f;
        public float startTime = 0;

        public bool isAutoSpawn;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (isAutoSpawn)
            {
                isAutoSpawn = false;

                Spawn();
            }
        }

        private void Spawn()
        {
            if (prefabs == null || prefabs.Length == 0)
                return;


            for (int i = 0; i < count; i++)
            {
                var p = Instantiate(prefabs[Random.Range(0, prefabs.Length)], transform);

                var pos = Random.insideUnitSphere * radius;
                pos.y = Random.Range(-y, y);
                p.transform.localPosition = pos;
            }
        }

        public void OnClick()
        {
            Spawn();
        }
    }
}