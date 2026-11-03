//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Shared;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Agent.Shared
{
    /// <summary>
    /// Defines connection and worker settings for a Gurux DLMS AMI agent.
    /// </summary>
    public class GXAgentOptions
    {
        /// <summary>
        /// Gets or sets the unique identifier of the agent.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the base address of the Gurux DLMS AMI server.
        /// </summary>
        /// <value>The server address. The default is <c>https://ami.gurux.fi</c>.</value>
        public string Address { get; set; } = "https://ami.gurux.fi";

        /// <summary>
        /// Gets or sets the personal access token used to authenticate with the server.
        /// </summary>
        /// <remarks>
        /// The agent can access meters that belong to the token owner.
        /// Treat this value as a secret.
        /// </remarks>
        public string Token { get; set; } = "";

        /// <summary>
        /// Gets or sets the diagnostic trace level for the agent.
        /// </summary>
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Gets or sets the agent version to run.
        /// </summary>
        /// <remarks>
        /// If the value is <see langword="null"/>, the default agent version is used.
        /// </remarks>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the settings for the meter reader worker.
        /// </summary>
        public ReaderSettings? ReaderSettings { get; set; }

        /// <summary>
        /// Gets or sets the listener settings for incoming meter connections.
        /// </summary>
        public ListenerSettings? ListenerSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for receiving notification, event, or push messages.
        /// </summary>
        public NotifySettings? NotifySettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for the worker that retrieves and parses stored raw notifications.
        /// </summary>
        public RawNotificationSettings? RawNotificationSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for accepting gateway connections.
        /// </summary>
        public GatewaySettings? GatewaySettings { get; set; }

        /// <summary>
        /// Gets or sets the serial port selected for meter communication.
        /// </summary>
        public string? SerialPort { get; set; }

        /// <summary>
        /// Returns a textual representation of the agent settings.
        /// </summary>
        /// <returns>A safe diagnostic summary; authentication and nested transport credentials are omitted.</returns>
        public override string ToString()
        {
            string address = "[invalid]";
            if (Uri.TryCreate(Address, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                address = new UriBuilder(uri) { UserName = "", Password = "", Query = "", Fragment = "" }.Uri.AbsoluteUri;
            }
            return $"Id: {Id}{Environment.NewLine}Address: {address}{Environment.NewLine}" +
                $"Token: [redacted]{Environment.NewLine}TraceLevel: {TraceLevel}{Environment.NewLine}" +
                $"Reader enabled: {ReaderSettings?.Enabled == true}{Environment.NewLine}" +
                $"Listener enabled: {ListenerSettings?.Enabled == true}{Environment.NewLine}" +
                $"Notifications enabled: {NotifySettings?.Enabled == true}{Environment.NewLine}" +
                $"Raw notifications enabled: {RawNotificationSettings?.Enabled == true}{Environment.NewLine}" +
                $"Gateway enabled: {GatewaySettings?.Enabled == true}";
        }

        /// <summary>Checks configuration before opening connections. Registration may omit the agent identifier.</summary>
        /// <param name="requireAgentId">Whether an already registered agent identifier is required.</param>
        /// <exception cref="GXAgentOptionsValidationException">Configuration is invalid. Structured errors contain stable codes and property paths without secret values.</exception>
        public void Validate(bool requireAgentId = true)
        {
            var errors = GetValidationErrors(requireAgentId);
            if (errors.Count != 0)
                throw new GXAgentOptionsValidationException(errors);
        }

        /// <summary>Returns all configuration errors without exposing secret values.</summary>
        /// <param name="requireAgentId">Whether an already registered agent identifier is required.</param>
        /// <returns>Errors with property paths suitable for configuration diagnostics.</returns>
        public IReadOnlyList<GXAgentValidationError> GetValidationErrors(bool requireAgentId = true)
        {
            var errors = new List<GXAgentValidationError>();
            void Error(GXAgentValidationErrorCode code, string name, string rule) => errors.Add(new GXAgentValidationError(code, name, rule));
            void Nonnegative(string name, int? value) { if (value < 0) Error(GXAgentValidationErrorCode.NegativeValue, name, "must not be negative."); }
            void Threads(string name, int value) { if (value < 1) Error(GXAgentValidationErrorCode.InvalidThreadCount, name, "must be at least one."); }
            void Media(string name, string? value) { if (string.IsNullOrWhiteSpace(value)) Error(GXAgentValidationErrorCode.MediaSettingsRequired, name, "is required when enabled."); }
            if (!Uri.TryCreate(Address, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                Error(GXAgentValidationErrorCode.InvalidAddress, nameof(Address), "must be an absolute HTTP or HTTPS address without credentials, query or fragment.");
            if (string.IsNullOrWhiteSpace(Token) || Token.Any(char.IsWhiteSpace))
                Error(GXAgentValidationErrorCode.InvalidToken, nameof(Token), "must contain a nonempty access token without whitespace.");
            if (requireAgentId && Id == Guid.Empty) Error(GXAgentValidationErrorCode.AgentIdRequired, nameof(Id), "is required after registration.");
            if (!string.IsNullOrEmpty(Version) && (Version.Length > 128 ||
                Version.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or '_' or '+'))))
                Error(GXAgentValidationErrorCode.InvalidVersion, nameof(Version), "must be a version identifier without path separators or special filename characters.");
            if (!Enum.IsDefined(TraceLevel)) Error(GXAgentValidationErrorCode.InvalidTraceLevel, nameof(TraceLevel), "is invalid.");
            if (ReaderSettings is { Enabled: true } reader)
            {
                Threads("ReaderSettings.Threads", reader.Threads);
                Nonnegative("ReaderSettings.TaskWaitTime", reader.TaskWaitTime);
                Nonnegative("ReaderSettings.AliveTime", reader.AliveTime);
            }
            if (ListenerSettings is { Enabled: true } listener)
            {
                Threads("ListenerSettings.Threads", listener.Threads);
                Media("ListenerSettings.MediaSettings", listener.MediaSettings);
                Nonnegative("ListenerSettings.IdentifyWaitTime", listener.IdentifyWaitTime);
                Nonnegative("ListenerSettings.ExpirationTime", listener.ExpirationTime);
                Nonnegative("ListenerSettings.ConnectionUpTime", listener.ConnectionUpTime);
            }
            if (NotifySettings is { Enabled: true } notify)
            {
                Threads("NotifySettings.Threads", notify.Threads);
                Media("NotifySettings.MediaSettings", notify.MediaSettings);
                Nonnegative("NotifySettings.ExpirationTime", notify.ExpirationTime);
                Nonnegative("NotifySettings.ConnectionUpTime", notify.ConnectionUpTime);
            }
            if (GatewaySettings is { Enabled: true } gateway)
            {
                Media("GatewaySettings.MediaSettings", gateway.MediaSettings);
                Nonnegative("GatewaySettings.IdentifyWaitTime", gateway.IdentifyWaitTime);
                Nonnegative("GatewaySettings.ExpirationTime", gateway.ExpirationTime);
            }
            if (RawNotificationSettings is { Enabled: true } raw)
            {
                Threads("RawNotificationSettings.Threads", raw.Threads);
                if (raw.ScriptMethod is null || raw.ScriptMethod == Guid.Empty)
                    Error(GXAgentValidationErrorCode.ScriptMethodRequired, "RawNotificationSettings.ScriptMethod", "is required when enabled.");
            }
            return errors.AsReadOnly();
        }
    }
}
