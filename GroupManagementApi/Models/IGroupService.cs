namespace GroupManagementApi.Models
{
    public interface IGroupService
    {
        Task<int> CreateGroupAsync(GroupCreateRequest request);

    }
}
