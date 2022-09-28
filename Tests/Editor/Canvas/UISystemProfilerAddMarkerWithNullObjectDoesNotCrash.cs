using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class UISystemProfilerAddMarkerWithNullObjectDoesNotCrash
    {
        [Test]
        public void AddMarkerShouldNotCrashWithNullObject()
        {
            UISystemProfilerApi.AddMarker("Test", null);
        }
    }
}
