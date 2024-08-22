namespace LumenicBackend.Controllers
{
    [Route("api/number")]
    [ApiController]
    public class NumberController : ControllerBase
    {
        private readonly INumberService numberService;

        public NumberController(INumberService numberService)
        {
            this.numberService = numberService;
        }

        [HttpPost]
        [Route("search")]
        public async Task<IActionResult> SearchForPhoneNumber(NumberRequest request)
        {
            var result = await numberService.SearchForPhoneNumber(request.AreaCode!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("purchase")]
        public async Task<IActionResult> PurchasePhoneNumber(PurchaseRequest purchaseRequest)
        {
            var result = await numberService.PurchasePhoneNumber(purchaseRequest);
            return this.StatusCode(result.Status);
        }

        [HttpPost]
        [Route("getNumbers")]
        public async Task<IActionResult> GetNumbers(NumberRequest request)
        {
            var result = numberService.GetNumbers(request.OrganizationId!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("release")]
        public async Task<IActionResult> ReleasePhoneNumber(NumberRequest request)
        {
            var result = await numberService.ReleasePhoneNumber(request.PhoneNumber!);
            return this.StatusCode(result.Status);
        }
    }
}
