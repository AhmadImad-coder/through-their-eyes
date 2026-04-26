using UnityEngine;

namespace OCDSimulation
{
    public static class RuntimePrimitive
    {
        public static GameObject Create(PrimitiveType type)
        {
            return GameObject.CreatePrimitive(type);
        }
    }
}
