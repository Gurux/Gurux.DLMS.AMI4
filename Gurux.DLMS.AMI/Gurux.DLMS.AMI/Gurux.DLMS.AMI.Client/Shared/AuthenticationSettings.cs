namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// External authentication settings.
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// Authentication handler.
        /// </summary>
        public string Name
        {
            get;
            set;
        } = "GitHub";

        /// <summary>
        /// Is authentication handler disabled.
        /// </summary>
        public bool Disabled
        {
            get;
            set;
        }

        /// <summary>
        /// Client ID.
        /// </summary>
        public string ClientId
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Cient secret.
        /// </summary>
        public string ClientSecret
        {
            get;
            set;
        } = "";
    }
}