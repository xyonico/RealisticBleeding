using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
    public struct FallingBloodDrop
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;
        public float LifetimeRemaining;

        private static EffectData _bloodDropDecalData;

        public FallingBloodDrop(Vector3 position, Vector3 velocity, float size, float lifetime)
        {
            Position = position;
            Velocity = velocity;
            Size = size;
            LifetimeRemaining = lifetime;
        }
        
        public static void OnFallingDropHitEnvironment(in FallingBloodDrop bloodDrop, Vector3 surfaceNormal)
        {
            if (_bloodDropDecalData == null)
            {
                _bloodDropDecalData = Catalog.GetData<EffectData>("DropBlood");
            }

            var rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), surfaceNormal) *
                           Quaternion.LookRotation(surfaceNormal);
            var instance = _bloodDropDecalData.Spawn(bloodDrop.Position, rotation);
            instance.Play();

            if (instance.effects == null || instance.effects.Count == 0) return;

            var effectDecal = instance.effects[0] as EffectDecal;

            if (effectDecal == null) return;

            const float decalScale = 0.1f;
            effectDecal.meshRenderer.transform.localScale = new Vector3(decalScale, decalScale, decalScale);
        }
    }
}