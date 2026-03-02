using System.Collections.Generic;
namespace GroupManagementApi.Models
{
    public class GroupCreateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<MemberDto> Members { get; set; }
    }
}
