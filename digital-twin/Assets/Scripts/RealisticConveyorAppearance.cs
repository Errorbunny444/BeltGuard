using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Finish recipes and part selection from your MetrologySimulation project's ConveyorPresentation.
// URP shader selected first for the Belt_Guard project. Geometry remains the existing CAD.
public static class RealisticConveyorAppearance
{
        public static void ApplyMaterials(Renderer[] renderers, Transform root)
        {
            var rubber = Material("Conveyor | graphite polyurethane belt", new Color(.07f, .078f, .08f), .02f, .22f);
            var aluminium = Material("Conveyor | satin aluminium extrusion", new Color(.49f, .52f, .55f), .8f, .38f);
            var stainless = Material("Conveyor | machined stainless steel", new Color(.59f, .62f, .65f), .88f, .55f);
            var coated = Material("Conveyor | graphite powder coat", new Color(.11f, .135f, .155f), .2f, .3f);
            var fastener = Material("Conveyor | black oxide fasteners", new Color(.095f, .11f, .12f), .76f, .36f);
            var motor = Material("Conveyor | anodised motor body", new Color(.075f, .085f, .1f), .6f, .32f);

            foreach (var renderer in renderers)
            {
                Material finish;
                string path = PathToRoot(renderer.transform, root);
                if (HasPart(renderer.transform, root, "belt")) finish = rubber;
                else if (path.Contains("screw") || path.Contains("snapring")) finish = fastener;
                else if (path.Contains("bearing") && !path.Contains("tensioner"))
                    finish = renderer.name == "Body10" ? fastener : stainless;
                else if (path.Contains("motorholder") || path.Contains("tensioner")) finish = coated;
                else if (HasPart(renderer.transform, root, "motor"))
                    finish = renderer.name == "Body1" || renderer.name == "Body3" || renderer.name == "Body2"
                        ? stainless : motor;
                else if (path.Contains("pulley") || path.Contains("shaft")) finish = stainless;
                else finish = aluminium;

                int count = Mathf.Max(1, renderer.sharedMaterials.Length);
                var slots = new Material[count];
                for (int i = 0; i < count; i++) slots[i] = finish;
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }

        private static Material Material(string name, Color colour, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var material = new Material(shader) { name = name, color = colour, enableInstancing = true };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Renderer[] SelectParts(Renderer[] all, Transform root, string part)
        {
            var result = new List<Renderer>();
            foreach (var renderer in all)
                if (HasPart(renderer.transform, root, part)) result.Add(renderer);
            return result.ToArray();
        }

        private static bool HasPart(Transform item, Transform root, string part)
        {
            for (var current = item; current != null && current != root; current = current.parent)
            {
                string name = current.name.ToLowerInvariant();
                int instance = name.IndexOf(':');
                if (instance >= 0) name = name.Substring(0, instance);
                if (name == part) return true;
            }
            return false;
        }

        private static string PathToRoot(Transform item, Transform root)
        {
            string path = "";
            for (var current = item; current != null && current != root; current = current.parent)
                path = current.name.ToLowerInvariant() + "/" + path;
            return path;
        }

}
