namespace Gurux.DLMS.AMI.Data
{
    public sealed class GroupInvite
    {
        public required string Token { get; init; }
        public required string GroupId { get; init; }
        public string? AllowedUser { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
        public DateTimeOffset ExpiresAtUtc { get; init; }
        public bool Used { get; set; }
    }
}
