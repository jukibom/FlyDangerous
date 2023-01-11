using System.Collections.Generic;
using System.Linq;
using Core.MapData.Serializable;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    public class GameModeBillboards : MonoBehaviour {
        [SerializeField] private BillboardSpawner billboardPrefab;

        public List<BillboardSpawner> BillboardSpawners { get; private set; } = new();

        public void RefreshBillboardSpawners() {
            BillboardSpawners.Clear();
            BillboardSpawners = GetComponentsInChildren<BillboardSpawner>().ToList();
        }

        public BillboardSpawner AddBillboard(SerializableBillboard serializableBillboard) {
            var billboardSpawner = Instantiate(billboardPrefab, transform);
            billboardSpawner.Deserialize(serializableBillboard);
            BillboardSpawners.Add(billboardSpawner);
            return billboardSpawner;
        }
    }
}