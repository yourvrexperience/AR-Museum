
using UnityEngine;

namespace yourvrexperience.Utils
{
    public static class ParticleExtensions
    {
        public static void Scale(this ParticleSystem particles, float scale, bool includeChildren = true) {
            ParticleScaler.Scale(particles, scale, includeChildren);
        }

        public static void ScaleByTransform(this ParticleSystem particles, float scale, bool includeChildren = true) {
            ParticleScaler.ScaleByTransform(particles, scale, includeChildren);
        }
    }

    public static class ParticleScaler
    {
        public static ParticleScalerOptions defaultOptions = new ParticleScalerOptions();

        static public void ScaleByTransform(ParticleSystem particles, float scale, bool includeChildren = true) {
            particles.scalingMode = ParticleSystemScalingMode.Local;
            particles.transform.localScale = particles.transform.localScale * scale;
            particles.gravityModifier *= scale;
            if (includeChildren) {
                var children = particles.GetComponentsInChildren<ParticleSystem>();
                for (var i = children.Length; i-- > 0;) {
                    if (children[i] == particles) { continue; }
                    children[i].scalingMode = ParticleSystemScalingMode.Local;
                    children[i].transform.localScale = children[i].transform.localScale * scale;
                    children[i].gravityModifier *= scale;
                }
            }
        }

        static public void Scale(ParticleSystem particles, float scale, bool includeChildren = true, ParticleScalerOptions options = null) {

            ScaleSystem(particles, scale, false, options);
            if (includeChildren) {
                var children = particles.GetComponentsInChildren<ParticleSystem>();
                for (var i = children.Length; i-- > 0;) {
                    if (children[i] == particles) { continue; }
                    ScaleSystem(children[i], scale, true, options);
                }
            }
        }

        private static void ScaleSystem(ParticleSystem particles, float scale, bool scalePosition, ParticleScalerOptions options = null) {
            if (options == null) { options = defaultOptions; }
            if (scalePosition) { particles.transform.localPosition *= scale; }
            
            particles.startSize *= scale;
            particles.gravityModifier *= scale;
            particles.startSpeed *= scale;

            if (options.shape) {
                var shape = particles.shape;
                shape.radius *= scale;
                shape.scale = shape.scale * scale;
            }
       }   

        private static ParticleSystem.MinMaxCurve ScaleMinMaxCurve(ParticleSystem.MinMaxCurve curve, float scale) {
            curve.curveMultiplier *= scale;
            curve.constantMin *= scale;
            curve.constantMax *= scale;
            ScaleCurve(curve.curveMin, scale);
            ScaleCurve(curve.curveMax, scale);
            return curve;
        }

        private static void ScaleCurve(AnimationCurve curve, float scale) {
            if (curve == null) { return; }
            for (int i = 0; i < curve.keys.Length; i++) { curve.keys[i].value *= scale; }
        }
    }

    public class ParticleScalerOptions
    {
        public bool shape = true;
        public bool velocity = true;
        public bool clampVelocity = true;
        public bool force = true;
    }
}