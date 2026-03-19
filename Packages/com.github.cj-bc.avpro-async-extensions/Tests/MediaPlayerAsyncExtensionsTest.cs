using System;
using System.Threading;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using AVProAsyncExtensions;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;

// TODO: Test all sort of events.
public class MediaPlayerAsyncExtensionsTest
{
    private static string k_SamplePath = "AVProVideoSamples/Cones-2D-1080p60-H264.mp4";

    private MediaPath _mediaPath;
    private MediaPlayer _player;

    [SetUp]
    public void Setup()
    {
        _player = new GameObject().AddComponent<MediaPlayer>();
        _player.AutoStart = false;
        _player.AutoOpen = false;
        _mediaPath = new(k_SamplePath, MediaPathType.RelativeToStreamingAssetsFolder);
    }

    [TearDown]
    public void TearDown()
    {
        _player.Stop();
        _player.CloseMedia();
    }

    [UnityTest]
    public IEnumerator AwaitMediaEvent_MetaDataReady() => UniTask.ToCoroutine(async () =>
    {
        using CancellationTokenSource cts = new();
        using var cancel = cts.CancelAfterSlim(TimeSpan.FromSeconds(10));

        await expectEvent(_player, MediaPlayerEvent.EventType.MetaDataReady, async () => _player.OpenMedia(_mediaPath, false), cts.Token);
        await UniTask.DelayFrame(10, cancellationToken: cts.Token);

        Assert.That(_player.Control.HasMetaData(), Is.True);
    });

    [UnityTest]
    public IEnumerator AwaitMediaEvent_ReadyToPlay() => UniTask.ToCoroutine(async () =>
    {
        using CancellationTokenSource cts = new();
        using var cancel = cts.CancelAfterSlim(TimeSpan.FromSeconds(10));

        await expectEvent(_player, MediaPlayerEvent.EventType.ReadyToPlay, async () => _player.OpenMedia(_mediaPath, false), cts.Token);
        await UniTask.DelayFrame(10, cancellationToken: cts.Token);

        Assert.That(_player.MediaOpened, Is.True);
        Assert.That(_player.Control.CanPlay(), Is.True);
    });

    [UnityTest]
    public IEnumerator AwaitMediaEvent_Started() => UniTask.ToCoroutine(async () =>
    {
        using CancellationTokenSource cts = new();
        using var cancel = cts.CancelAfterSlim(TimeSpan.FromSeconds(20));

        await expectEvent(_player, MediaPlayerEvent.EventType.Started, async () => _player.OpenMedia(_mediaPath, true), cts.Token);
        await UniTask.DelayFrame(10, cancellationToken: cts.Token);

        Assert.That(_player.MediaOpened, Is.True);
        Assert.That(_player.Control.IsPlaying(), Is.True);
    });

    [UnityTest]
    public IEnumerator AwaitMediaEvent_FirstFrameReady() => UniTask.ToCoroutine(async () =>
    {
        using CancellationTokenSource cts = new();
        using var cancel = cts.CancelAfterSlim(TimeSpan.FromSeconds(20));

        await expectEvent(_player, MediaPlayerEvent.EventType.FirstFrameReady, async () => _player.OpenMedia(_mediaPath, false), cts.Token);
        await UniTask.DelayFrame(10, cancellationToken: cts.Token);

        Assert.That(_player.MediaOpened, Is.True);
        Assert.That(_player.Control.CanPlay(), Is.True);
    });

    [UnityTest]
    public IEnumerator AwaitMediaEvent_FinishedPlaying() => UniTask.ToCoroutine(async () =>
    {
        using CancellationTokenSource cts = new();
        using var cancel = cts.CancelAfterSlim(TimeSpan.FromSeconds(50));

        await expectEvent(_player, MediaPlayerEvent.EventType.FinishedPlaying, async () => _player.OpenMedia(_mediaPath, true), cts.Token);
        await UniTask.DelayFrame(10, cancellationToken: cts.Token);

        Assert.That(_player.MediaOpened, Is.True);
        Assert.That(_player.Control.IsPlaying(), Is.False);
        Assert.That(_player.Control.IsFinished(), Is.True);
    });

    [UnityTest]
    public IEnumerator AwaitMediaEvent_Closing() => UniTask.ToCoroutine(async () =>
    {
        using CancellationTokenSource cts = new();
        using var cancel = cts.CancelAfterSlim(TimeSpan.FromSeconds(50));

        await expectEvent(_player, MediaPlayerEvent.EventType.Closing, async () =>
        {
            _player.OpenMedia(_mediaPath, false);
            await UniTask.WaitUntil(() => _player.MediaOpened);
            _player.CloseMedia();
        }, cts.Token);
        await UniTask.DelayFrame(10, cancellationToken: cts.Token);

        Assert.That(_player.MediaOpened, Is.False);
        Assert.That(_player.Control.IsPlaying(), Is.False);
    });

    private async UniTask expectEvent(MediaPlayer player, MediaPlayerEvent.EventType evType, Func<UniTask> f, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        bool eventTriggered = false;
        UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode> receiveEvent = UniTask.UnityAction(async (MediaPlayer pl, MediaPlayerEvent.EventType ev, ErrorCode err) =>
        {
            if (ev == evType)
            {
                eventTriggered = true;
            }
        });

        try
        {
            player.Events.AddListener(receiveEvent);
            var mediaEventTask = player.AwaitMediaEvent(evType, cts.Token).SuppressCancellationThrow();
            f();

            bool canceled = await mediaEventTask;

            Assert.That(canceled, Is.False);
            Assert.That(eventTriggered, Is.True);
        } finally
        {
            player.Events.RemoveListener(receiveEvent);
        }
    }
}
