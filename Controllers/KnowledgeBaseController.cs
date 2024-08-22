namespace LumenicBackend.Controllers
{
    [Route("api/knowledgebase")]
    [ApiController]
    public class KnowledgeBaseController : ControllerBase
    {
        private readonly IKnowledgeBaseService knowledgeBaseService;

        public KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService)
        {
            this.knowledgeBaseService = knowledgeBaseService;
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddKnowledgeBaseEntry(KnowledgeBaseRequest request)
        {
            var usage = await this.knowledgeBaseService.AddKnowledgeBase(request);
            return new JsonResult(usage);
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateKnowledgeBaseEntry(KnowledgeBaseRequest request)
        {
            var usage = await this.knowledgeBaseService.UpdateKnowledgeBase(request);
            return new JsonResult(usage);
        }

        [HttpPost]
        [Route("search")]
        public async Task<IActionResult> SearchKnowledgeBase(KnowledgeBaseRequest request)
        {
            var result = await this.knowledgeBaseService.SearchKnowledgeBase(request);
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("getAllIndexes")]
        public async Task<IActionResult> GetAllIndexes()
        {
            var result = await this.knowledgeBaseService.GetAllKnowledgeBaseIndexes();
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteKnowledgeBaseIndex(KnowledgeBaseRequest request)
        {
            await this.knowledgeBaseService.DeleteKnowledgeBase(request.Index!, request.OrganizationId!);
            return Ok();
        }
    }
}
