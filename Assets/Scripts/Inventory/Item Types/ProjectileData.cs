﻿using System;
using UnityEngine;

namespace Inventory.Inventory.Item_Types
{
    [Serializable]
    public class ProjectileData
    {
        public Sprite sprite;
        public float projectileSpeed;
        public float damage;
        public bool piercing;
        public bool useGravity;
        public bool collideWithWorld;
        public bool canHurtPlayer;
    }
}