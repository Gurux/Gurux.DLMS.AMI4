using System.Collections.Concurrent;

namespace Gurux.DLMS.AMI.Data
{
    public sealed class InMemoryInviteStore : IInviteStore
    {
        private readonly ConcurrentDictionary<string, GroupInvite> _invites = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _members = new();

        public GroupInvite Create(string userGroupId, string? userId, TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromHours(24))
                ttl = TimeSpan.FromHours(24);

            var now = DateTimeOffset.UtcNow;
            var inv = new GroupInvite
            {
                Token = TokenGenerator.CreateUrlSafeToken(),
                GroupId = userGroupId,
                AllowedUser = string.IsNullOrWhiteSpace(userId) ? null : userId,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(ttl),
                Used = false
            };
            _invites[inv.Token] = inv;
            return inv;
        }

        public GroupInvite? Get(string token) => _invites.TryGetValue(token, out var x) ? x : null;

        public bool TryRedeem(string token, string currentUser, out GroupInvite? invite, out string message)
        {
            invite = null; message = "";
            if (!_invites.TryGetValue(token, out var inv)) { message = "Invite not found."; return false; }

            if (inv.Used) { message = "Invite already used."; return false; }
            if (DateTimeOffset.UtcNow > inv.ExpiresAtUtc) { message = "Invite expired."; return false; }
            if (!string.IsNullOrEmpty(inv.AllowedUser) && !string.Equals(inv.AllowedUser, currentUser, StringComparison.OrdinalIgnoreCase))
            {
                message = "Invite is restricted to another user.";
                return false;
            }

            // merkkaa käytetyksi (kertakäyttö)
            inv.Used = true;
            AddMembership(inv.GroupId, currentUser);
            invite = inv;
            message = "Joined to user group.";
            return true;
        }

        public IEnumerable<GroupInvite> All() => _invites.Values;

        public void AddMembership(string groupId, string user)
        {
            var set = _members.GetOrAdd(groupId, _ => new());
            set[user] = 1;
        }

        public IReadOnlyCollection<string> Members(string groupId)
        {
            if (_members.TryGetValue(groupId, out var set))
                return set.Keys.ToList();
            return Array.Empty<string>();
        }
    }
}
