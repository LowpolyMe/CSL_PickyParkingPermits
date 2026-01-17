using UnityEngine;

namespace PickyParking.Logging
{
    public sealed class LogFlushPump : MonoBehaviour
    {
        private const float FlushIntervalSeconds = 0.25f;
        private float _nextFlushTime;

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now < _nextFlushTime)
                return;

            _nextFlushTime = now + FlushIntervalSeconds;
            Log.FlushQueuedMessagesFromMainThread();
        }
    }
}
