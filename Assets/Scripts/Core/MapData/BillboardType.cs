using System.Collections.Generic;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class BillboardType : IFdEnum {
        private static int _id;

        private static readonly BillboardData FlyDangerousBillboard = new BillboardWithTextureData
            { Name = "Fly Dangerous", TextureResourceName = "Billboard Fly Dangerous", ColorIntensity = 8, ScrollSpeed = 0.2f };

        private static readonly BillboardData CustomMessageBillboard = new BillboardWithMessageData { Name = "Custom Message", ColorIntensity = 8 };

        private static readonly BillboardData DirectionArrowSingleBillboard = new BillboardWithTextureData {
            Name = "Direction Arrow Single", TextureResourceName = "Billboard Arrow Single", Tint = new Color(1, 1, 0), ColorIntensity = 35, ScrollSpeed = 0.2f
        };

        private static readonly BillboardData DirectionArrowDoubleBillboard = new BillboardWithTextureData {
            Name = "Direction Arrow Double", TextureResourceName = "Billboard Arrow Double", Tint = new Color(1, 1, 0), ColorIntensity = 35, ScrollSpeed = 0.2f
        };

        private static readonly BillboardData SquidColaBillboard = new BillboardWithTextureData
            { Name = "Squid Cola", TextureResourceName = "Billboard Squid Cola", ColorIntensity = 16 };

        private static readonly BillboardData CopeBillboard = new BillboardWithTextureData
            { Name = "Cope", TextureResourceName = "Billboard Cope", ColorIntensity = 8, ScrollSpeed = 0 };

        private static readonly BillboardData NewtonsGambitBillboard = new BillboardWithTextureData
            { Name = "Newtons Gambit", TextureResourceName = "Billboard Newtons Gambit", ColorIntensity = 12 };

        private static readonly BillboardData EdenPrimeBillboard = new BillboardWithTextureData
            { Name = "Eden Prime", TextureResourceName = "Billboard Eden Prime", ColorIntensity = 8 };

        private static readonly BillboardData EuropaBillboard = new BillboardWithTextureData
            { Name = "Europa", TextureResourceName = "Billboard Europa", ColorIntensity = 8 };


        public static readonly BillboardType FlyDangerous = new(FlyDangerousBillboard);
        public static readonly BillboardType CustomMessage = new(CustomMessageBillboard);
        public static readonly BillboardType DirectionArrowSingle = new(DirectionArrowSingleBillboard);
        public static readonly BillboardType DirectionArrowDouble = new(DirectionArrowDoubleBillboard);
        public static readonly BillboardType SquidCola = new(SquidColaBillboard);
        public static readonly BillboardType Cope = new(CopeBillboard);
        public static readonly BillboardType NewtonsGambit = new(NewtonsGambitBillboard);
        public static readonly BillboardType EdenPrime = new(EdenPrimeBillboard);
        public static readonly BillboardType Europa = new(EuropaBillboard);

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
                DirectionArrowSingle,
                DirectionArrowDouble,
                SquidCola,
                Cope,
                NewtonsGambit,
                EdenPrime,
                Europa
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