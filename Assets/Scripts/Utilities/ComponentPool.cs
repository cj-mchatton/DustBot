using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    /// <summary>
    /// Reusable pool for future sparkles, tile effects, and temporary UI labels.
    /// Gameplay itself does not Instantiate or Destroy objects while a route runs.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> available = new Stack<T>();

        public ComponentPool(T prefab, Transform parent, int preload)
        {
            if (prefab == null)
            {
                throw new System.ArgumentNullException("prefab");
            }

            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < preload; i++)
            {
                T instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(false);
                available.Push(instance);
            }
        }

        public T Get()
        {
            T instance = available.Count > 0 ? available.Pop() : Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(parent, false);
            available.Push(instance);
        }
    }
}
