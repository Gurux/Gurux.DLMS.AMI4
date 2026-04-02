namespace Gurux.DLMS.AMI.Client.Helpers.Toaster
{
    /// <summary>
    /// Toast.
    /// </summary>
    public record GXToast
    {
        /// <summary>
        /// Toast ID.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// Toast title.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Toast message.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Toast color.
        /// </summary>
        public Color Color { get; init; } = Color.Primary;

        /// <summary>
        /// Creation time.
        /// </summary>
        public readonly DateTimeOffset CreationTime = DateTimeOffset.Now;

        /// <summary>
        /// Closing time.
        /// </summary>
        public DateTimeOffset? ClosingTime { get; init; }

        /// <summary>
        /// Is time elapsed and toaster should remove.
        /// </summary>
        public bool IsElapsed
        {
            get
            {
                return ClosingTime < DateTimeOffset.Now;
            }
        }

        /// <summary>
        /// Get posted time text.
        /// </summary>
        public string PostedTimeText
        {
            get
            {
                TimeSpan elapsedTime = CreationTime - DateTimeOffset.Now;
                return elapsedTime.Seconds > 60
                ? $"posted {-elapsedTime.Minutes} mins ago"
                : $"posted {-elapsedTime.Seconds} secs ago";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXToast()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        /// <param name="messageColour">Color</param>
        /// <param name="secsToLive">Lifetime</param>
        public GXToast(string title, string? message, Color messageColour, int secsToLive)
        {
            Title = title;
            if (message != null)
            {
                Message = message.Replace(Environment.NewLine, "<br/>");
            }
            Color = messageColour;
            ClosingTime = DateTimeOffset.Now.AddSeconds(secsToLive);
        }
    }
}