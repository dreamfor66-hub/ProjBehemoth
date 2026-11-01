namespace DungeonSlash
{
    public enum MerchantRoomEvent
    {
        Offer,
        GoneMessage,
        Finished
    }

    public enum FountainRoomEvent
    {
        Offer,
        DriedMessage,
        Finished
    }

    /// <summary>Persistent per-room event flags for the current run only.</summary>
    public sealed class RoomEventProgress
    {
        public bool MerchantDeparted { get; private set; }
        public bool MerchantGoneMessageShown { get; private set; }
        public bool MerchantPurchased { get; private set; }
        public bool HasLeftSinceFirstVisit { get; private set; }
        public bool ConversationStarted { get; private set; }
        public bool ConversationIgnored { get; private set; }
        private bool rearApproachArmed;
        public bool FountainDrained { get; private set; }
        public bool FountainDriedMessageShown { get; private set; }

        public void MarkMerchantDeparted() => MerchantDeparted = true;
        public void MarkMerchantPurchase() => MerchantPurchased = true;
        public void BeginConversation() { ConversationStarted = true; ConversationIgnored = false; rearApproachArmed = false; }
        public void IgnoreConversation() => ConversationIgnored = true;
        public void LeaveRoom()
        {
            HasLeftSinceFirstVisit = true;
            rearApproachArmed = !ConversationStarted || ConversationIgnored;
            if (MerchantPurchased) MerchantDeparted = true;
        }
        public bool CanRearApproach => HasLeftSinceFirstVisit && rearApproachArmed;

        public MerchantRoomEvent ConsumeMerchantVisit()
        {
            if (!MerchantDeparted) return MerchantRoomEvent.Offer;
            if (MerchantGoneMessageShown) return MerchantRoomEvent.Finished;
            MerchantGoneMessageShown = true;
            return MerchantRoomEvent.GoneMessage;
        }

        public void DrinkFromFountain() => FountainDrained = true;

        public FountainRoomEvent ConsumeFountainVisit()
        {
            if (!FountainDrained) return FountainRoomEvent.Offer;
            if (FountainDriedMessageShown) return FountainRoomEvent.Finished;
            FountainDriedMessageShown = true;
            return FountainRoomEvent.DriedMessage;
        }
    }
}
