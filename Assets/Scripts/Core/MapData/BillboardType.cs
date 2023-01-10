using System.Collections.Generic;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class BillboardType : IFdEnum {
        private static int _id;

        private static readonly BillboardData FlyDangerousBillboard = new BillboardWithTextureData
            { Name = "Fly Dangerous", TextureResourceName = "Billboard Fly Dangerous", ColorIntensity = 8, ScrollSpeed = 0.2f };

        private static readonly BillboardData CustomMessageBillboard = new BillboardWithMessageData { Name = "Custom Message" };

        private static readonly BillboardData DirectionBillboard = new BillboardWithTextureData
            { Name = "Direction", TextureResourceName = "Billboard Arrow", Tint = new Color(1, 1, 0), ColorIntensity = 5, ScrollSpeed = 0.2f };

        private static readonly BillboardData SquidColaBillboard = new BillboardWithTextureData
            { Name = "Squid Cola", TextureResourceName = "Billboard Squid Cola", ColorIntensity = 4 };

        private static readonly BillboardData CopeBillboard = new BillboardWithTextureData
            { Name = "Cope", TextureResourceName = "Billboard Cope", ColorIntensity = 8, ScrollSpeed = 0 };

        public static readonly BillboardType FlyDangerous = new(FlyDangerousBillboard);
        public static readonly BillboardType CustomMessage = new(CustomMessageBillboard);
        public static readonly BillboardType Direction = new(DirectionBillboard);
        public static readonly BillboardType SquidCola = new(SquidColaBillboard);
        public static readonly BillboardType Cope = new(CopeBillboard);

        private BillboardType(BillboardData billboardData) {
            Id = GenerateId;
            BillboardData = billboardData;
        }

        private static int GenerateId => _id++;

        public int Id { get; }
        public string Name => BillboardData.Name;
        public BillboardData BillboardData { get; }

        public static IEnumerable<BillboardType> List() {
            return new[] {
                FlyDangerous,
                CustomMessage,
                Direction,
                SquidCola,
                Cope
            };
        }

        public static BillboardType FromString(string billboardName) {
            return FdEnum.FromString(List(), billboardName);
        }

        public static BillboardType FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}