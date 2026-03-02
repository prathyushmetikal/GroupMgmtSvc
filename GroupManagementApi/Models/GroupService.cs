using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using System.Text.Json;

namespace GroupManagementApi.Models
{
    public class GroupService:IGroupService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GroupService> _logger;
        private readonly IAmazonEventBridge _eventBridge;

        public GroupService(
            IConfiguration configuration,
            ILogger<GroupService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _eventBridge = new AmazonEventBridgeClient();
        }

        //public async Task<int> CreateGroupAsync(GroupCreateRequest request)
        //{
        //    _logger.LogInformation("Creating group {GroupName}", request.Name);

        //    var connectionString = _configuration.GetConnectionString("GroupDb");

        //    await using var conn = new NpgsqlConnection(connectionString);
        //    await conn.OpenAsync();

        //    await using var cmd = new NpgsqlCommand(
        //      "SELECT add_members_to_group_v2(@group_name, @members, @created_by);",conn);

        //    cmd.Parameters.Add(new NpgsqlParameter("group_name", NpgsqlTypes.NpgsqlDbType.Text)
        //    {
        //        Value = request.Name
        //    });

        //    var membersJson = JsonSerializer.Serialize(
        //        request.Members.Select(m => new
        //        {
        //            full_name = m.Full_Name,
        //            email = m.Email,
        //            role = "Member"
        //        }));

        //    cmd.Parameters.Add(new NpgsqlParameter("members", NpgsqlTypes.NpgsqlDbType.Jsonb)
        //    {
        //        Value = membersJson
        //    });

        //    cmd.Parameters.Add(new NpgsqlParameter("created_by", NpgsqlTypes.NpgsqlDbType.Integer)
        //    {
        //        Value = 1
        //    });

        //    await cmd.ExecuteNonQueryAsync();


        //    await using var getCmd = new NpgsqlCommand(
        //        "SELECT group_id FROM groups WHERE group_name=@name",
        //        conn);

        //    getCmd.Parameters.AddWithValue("name", request.Name);

        //    var groupId = (int)(await getCmd.ExecuteScalarAsync());

        //    _logger.LogInformation("Group created with ID {GroupId}", groupId);

        //    await PublishEvent(groupId, request);

        //    return groupId;
        //}
        public async Task<int> CreateGroupAsync(GroupCreateRequest request)
        {
            _logger.LogInformation("Creating group {GroupName}", request.Name);

            var connectionString = _configuration.GetConnectionString("GroupDb");

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT add_members_to_group_v2(@group_name, @members, @created_by);",
                conn);

            cmd.Parameters.AddWithValue("group_name", request.Name);

            var membersJson = JsonSerializer.Serialize(
                request.Members.Select(m => new
                {
                    full_name = m.Name,
                    email = m.Email,
                    role = "Member"
                }));

            cmd.Parameters.Add("members", NpgsqlTypes.NpgsqlDbType.Jsonb)
               .Value = membersJson;

            cmd.Parameters.AddWithValue("created_by", 1);
            _logger.LogInformation("Members JSON being sent: {Json}", membersJson);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                throw new Exception("Group creation failed. Function returned null.");

            var groupId = Convert.ToInt32(result);

            _logger.LogInformation("Group created with ID {GroupId}", groupId);

            await PublishEvent(groupId, request);

            return groupId;
        }

        private async Task PublishEvent(int groupId, GroupCreateRequest request)
        {
            var detail = new
            {
                groupId,
                groupName = request.Name,
                memberCount = request.Members?.Count ?? 0
            };

            var putRequest = new PutEventsRequest
            {
                Entries = new List<PutEventsRequestEntry>
            {
                new PutEventsRequestEntry
                {
                    Source = "group.service",
                    DetailType = "GroupCreated",
                    Detail = JsonSerializer.Serialize(detail),
                    EventBusName = "default"
                }
            }
            };

            await _eventBridge.PutEventsAsync(putRequest);

            _logger.LogInformation("EventBridge event published for group {GroupId}", groupId);
        }
    }
}
