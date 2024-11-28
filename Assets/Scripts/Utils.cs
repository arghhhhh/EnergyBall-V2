using UnityEngine;

namespace Assets.Scripts
{
    public static class Utils
    {
        public static float Remap(
            float input,
            float fromMin,
            float fromMax,
            float toMin,
            float toMax
        )
        {
            float fromAbs = input - fromMin;
            float fromMaxAbs = fromMax - fromMin;

            float normal = fromAbs / fromMaxAbs;

            float toMaxAbs = toMax - toMin;
            float toAbs = toMaxAbs * normal;

            float to = toAbs + toMin;

            return to;
        }

        public static float RemapClamped(
            float input,
            float fromMin,
            float fromMax,
            float toMin,
            float toMax
        )
        {
            float fromAbs = input - fromMin;
            float fromMaxAbs = fromMax - fromMin;

            float normal = fromAbs / fromMaxAbs;

            float toMaxAbs = toMax - toMin;
            float toAbs = toMaxAbs * normal;

            float to = toAbs + toMin;

            if (to < toMin)
                to = toMin;
            else if (to > toMax)
                to = toMax;

            return to;
        }

        public static float GetVector3Avg(Vector3 transform)
        {
            return (transform.x + transform.y + transform.z) / 3f;
        }

        public static Vector3 FloatToVector3(float f)
        {
            return new Vector3(f, f, f);
        }
    }
}
