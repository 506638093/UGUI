using NUnit.Framework;
using UnityEngine;

public class RectTransformPosition
{
    [Test]
    public void SettingPositionBeforeGameObjectIsActivatedWorks_953409()
    {
        var positionToSet = new Vector3(1, 2, 3);
        var go = new GameObject("RectTransform", typeof(RectTransform));

        go.SetActive(false);
        go.transform.position = positionToSet;
        go.SetActive(true);

        Assert.AreEqual(positionToSet, go.transform.position, "Expected RectTransform position to be set but it was not.");
    }
}
