using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ContainerAggregator.Interfaces;
using Microsoft.ServiceFabric.Samples.Utility;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Generator;
using System.Text;

namespace WebFront.Controllers
{   
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private ILivenessCounter<string> counter;
        private IContainerAggregator proxy;

        public ValuesController(ILivenessCounter<string> counter, Uri serviceUri)
        {
            this.counter = counter;
            proxy = ActorServiceProxy.Create<IContainerAggregator>(serviceUri, 0);
        }

        // GET api/values/getAllKeys
        public JsonResult Index()
        {
            var localResults = counter.GetKeyValues();
            var globalResults = proxy.GetKeyValues().GetAwaiter().GetResult();

            var combinedResults = new ReportedResults();
            combinedResults.GlobalReports = globalResults;
            combinedResults.LocalReports = localResults;

            return Json(combinedResults);
        }

        // GET api/values/getContainerCount
        [HttpGet("{id}")]
        public string getContainerCount()
        {   
            string count = "0";
            try
            {
                count = proxy.GetLivingCount().GetAwaiter().GetResult().ToString();
            }
            catch (Exception)
            {
                // Do nothing if actor service is down.
            }

            Console.WriteLine("Serving up getContainerCount");
            return count;
        }        

        // POST api/values
        [HttpPost]
        public ActionResult Post([FromQuery]string id)
        {
            counter.ReportAlive(id);
            var host = this.HttpContext.Request.Host;
            Console.WriteLine($"Got ping with id: {id}, hostInfo: {host.ToString()}");
            return Ok();
        }

        // DELETE api/values
        [HttpDelete]
        public ActionResult Delete([FromQuery]string id, [FromQuery]int ignoreOtherUpdatesForMins)
        {
            // the DELETE is for a node to signal failover and must set the count to 0 for specified mins and ignore actual updates from node.
            proxy.ReportAlive(id, 0, ignoreOtherUpdatesForMins).GetAwaiter().GetResult();
            return Ok();
        }

        [HttpGet()]
        [Route("updatelogs")]
        public JsonResult getUpdateLogs()
        {   
            Console.WriteLine("Serving up getUpdateLogs");
            var updateLogEntries = proxy.GetUpdateLogEntries().GetAwaiter().GetResult();

            return Json(updateLogEntries.Reverse());
        }
    }
}

