namespace Gurux.DLMS.AMI.Data
{
    public interface IInviteStore
    {
        GroupInvite Create(string groupId, string allowedUserId, TimeSpan ttl);
        GroupInvite? Get(string token);
        bool TryRedeem(string token, string currentUser, out GroupInvite? invite, out string message);
        IEnumerable<GroupInvite> All();
        void AddMembership(string groupId, string user);
        IReadOnlyCollection<string> Members(string groupId);
    }
}
