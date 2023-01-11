using JetBrains.Annotations;
using UnityEngine;

namespace Core.MapData {
    public enum BillboardMode {
        Message,
        Texture
    }

    public class BillboardData {
        public string Name { get; set; }
        public virtual BillboardMode BillboardMode { get; set; }
        public float ScrollSpeed { get; set; } = 0;
        public Color Tint { get; set; } = Color.white;
        public float ColorIntensity { get; set; } = 2;
        [CanBeNull] public virtual string Message { get; set; }
        [CanBeNull] public virtual string TextureResourceName { get; set; }
    }

    public class BillboardWithMessageData : BillboardData {
        public override BillboardMode BillboardMode => BillboardMode.Message;
        public override string Message { get; set; }
    }

    public class BillboardWithTextureData : BillboardData {
        public override BillboardMode BillboardMode => BillboardMode.Texture;
        public override string TextureResourceName { get; set; }
    }
}