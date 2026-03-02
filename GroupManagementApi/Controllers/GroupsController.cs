using GroupManagementApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace GroupManagementApi.Controllers
{
    [ApiController]
    [Route("api/groups")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(
            IGroupService groupService,
            ILogger<GroupsController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(
            [FromBody] GroupCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Invalid group creation request");
                return BadRequest("Invalid group data");
            }

            try
            {
                var groupId = await _groupService.CreateGroupAsync(request);

                return CreatedAtAction(nameof(CreateGroup),
                    new { id = groupId },
                    new { groupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
