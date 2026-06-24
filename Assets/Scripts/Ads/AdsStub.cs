namespace DustBot
{
    public interface IRewardedHintProvider
    {
        bool IsRewardedHintAvailable { get; }
        void ShowRewardedHint(System.Action<bool> onCompleted);
    }

    // Deliberately non-functional until a real, consent-aware ad SDK is selected.
    public sealed class AdsStub : IRewardedHintProvider
    {
        public bool IsRewardedHintAvailable
        {
            get { return false; }
        }

        public void ShowRewardedHint(System.Action<bool> onCompleted)
        {
            if (onCompleted != null)
            {
                onCompleted(false);
            }
        }
    }
}
