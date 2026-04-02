namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Language settings.
    /// </summary>
    public class LanguageSettings
    {
        /// <summary>
        /// Language Id.
        /// </summary>
        public string? Id
        {
            get;
            set;
        }

        /// <summary>
        /// English name.
        /// </summary>
        public string? EnglishName
        {
            get;
            set;
        }

        /// <summary>
        /// Native name.
        /// </summary>
        public string? NativeName
        {
            get;
            set;
        }
        /// <summary>
        /// When the localized resource are updated for the last time.
        /// </summary>
        public DateTimeOffset? Updated { get; set; }
    }
}