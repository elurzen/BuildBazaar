using Microsoft.AspNetCore.Mvc;

namespace BuildBazaarCore.Controllers
{

    public class HomeController : Controller
    {
        //private readonly string ENVIRONMENT;
        //private readonly IBuildBazaarConfigService _configService;
        //private readonly IWebHostEnvironment _webHostEnvironment;
        //private readonly IAmazonS3 _s3Client;

        public HomeController()
        {
            //_configService = configService;
            //_webHostEnvironment = webHostEnvironment;

            //// Initialize the S3 client using the AWS region
            //_s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(
            //    _configService.GetConfigValue("REGION") ?? Amazon.RegionEndpoint.USEast1.SystemName));            
        }

        [Route("")]
        [Route("ResetPassword/{resetToken}")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("/Builds/{buildId?}")]
        public ActionResult Builds()
        {
            return View();
        }

        [Route("Public/{username?}/{buildId?}")]
        public ActionResult Public()
        {
            return View();
        }
    }       
}
                