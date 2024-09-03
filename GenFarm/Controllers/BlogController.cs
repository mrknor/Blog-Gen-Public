using Microsoft.AspNetCore.Mvc;
using GenFarm.Services;

namespace GenFarm.Controllers
{
    [ApiController]
    [Route("api/blog")]
    public class BlogController : ControllerBase
    {

        private readonly BlogBuilderService _blogBuilderService;
        private readonly BackgroundTaskQueue _taskQueue;

        public BlogController(BlogBuilderService blogBuilderService, BackgroundTaskQueue taskQueue)
        {
           
            _blogBuilderService = blogBuilderService;
            _taskQueue = taskQueue;
        }

        [HttpPost("generate")]
        public IActionResult GenerateBlog([FromBody] BlogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SEOPhrase))
            {
                return BadRequest("SEO phrase is required.");
            }

            var taskId = _blogBuilderService.QueueBlogGeneration(request.SEOPhrase);

            return Accepted(new { TaskId = taskId });
        }

        [HttpGet("status/{taskId}")]
        public IActionResult CheckTaskStatus(Guid taskId)
        {
            var status = _taskQueue.GetTaskStatus(taskId);
            return Ok(new { TaskId = taskId, Status = status });
        }


    }

}
