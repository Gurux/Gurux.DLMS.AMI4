using System.Data;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

partial class EnumTypeRepository
{
    /// <summary>Registers a distinct namespace for agent states without changing legacy event IDs or log references.</summary>
    internal static async Task EnsureAgentStatusesAsync(GXDbConnection connection, IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var existing = await connection.SelectAsync<GXEnumType>(transaction,
            GXSelectArgs.SelectAll<GXEnumType>(e => e.Type == TargetType.AgentStatus), cancellationToken);
        foreach (var status in Enum.GetValues<AgentStatus>())
        {
            string name = status.ToString();
            if (!existing.Any(e => e.Name == name))
            {
                await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXEnumType
                {
                    Type = TargetType.AgentStatus,
                    Name = name,
                    Value = (int)status
                }), cancellationToken);
            }
        }
    }
}