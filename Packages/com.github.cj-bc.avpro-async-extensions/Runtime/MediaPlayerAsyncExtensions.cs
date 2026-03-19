using System.Threading;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using UnityEngine.Events;

namespace AVProAsyncExtensions
{
    public static class MediaPlayerAsyncExtensions
    {
        public static UniTask AwaitMediaEvent(this MediaPlayer self, MediaPlayerEvent.EventType eventType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new UniTaskCompletionSource();

            CancellationTokenRegistration registration = default;

            UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode> onEvent = default;
            onEvent = (_, type, __) =>
            {
                if (type != eventType) return;
                self.Events.RemoveListener(onEvent);
                registration.Dispose();
                tcs.TrySetResult();
            };

            self.Events.AddListener(onEvent);

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.RegisterWithoutCaptureExecutionContext((state) =>
                {
                    (var player, var tcs, var token, var onEvent) =
                        ((MediaPlayer player, UniTaskCompletionSource tcs, CancellationToken token, UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode> onEvent))state;
                    player.Events.RemoveListener(onEvent);
                    tcs.TrySetCanceled(token);
                }, (self, tcs, cancellationToken, onEvent));
            }
        
            return tcs.Task;
        }
    }

}
