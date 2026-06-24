using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    public static class AnalyticsStub
    {
        public static void Track(string eventName, Dictionary<string, object> parameters = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (parameters == null)
            {
                Debug.Log("[AnalyticsStub] " + eventName);
            }
            else
            {
                Debug.Log(string.Format("[AnalyticsStub] {0} ({1} fields)", eventName, parameters.Count));
            }
#endif
        }
    }
}

