using System;
using System.Threading;
using NUnit.Framework;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpriteResourceLifecycleTests
{
    [Test]
    public void PublicSetSubSpriteKeepsPositionalCancellationAndEmptyInputGuards()
    {
        Image image = null;
        SpriteRenderer renderer = null;
        image.SetSprite(string.Empty, false, null, CancellationToken.None);
        renderer.SetSprite(string.Empty, null, CancellationToken.None);
        image.SetSubSprite(string.Empty, "icon", false, CancellationToken.None);
        image.SetSubSprite("atlas", string.Empty, false, CancellationToken.None);
        renderer.SetSubSprite(string.Empty, "icon", CancellationToken.None);
        renderer.SetSubSprite("atlas", string.Empty, CancellationToken.None);
    }
}
