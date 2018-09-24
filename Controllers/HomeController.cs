using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskRouterDotNetCore.Models;
using Microsoft.Extensions.Configuration;
using Twilio.Rest.Taskrouter.V1.Workspace;
using Twilio;
using Twilio.Rest.Api.V2010.Account.Conference;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using Twilio.Jwt.Taskrouter;
using Twilio.Jwt.Client;
using Twilio.Http;
using Twilio.Jwt;
using Newtonsoft.Json;

namespace TaskRouterDotNetCore.Controllers
{
    public class HomeController : Controller
    {
        protected string _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACME_ACCOUNT_SID");
        protected string  _authToken = Environment.GetEnvironmentVariable("TWILIO_ACME_AUTH_TOKEN");
        protected string _applicationSid =  Environment.GetEnvironmentVariable("TWILIO_ACME_TWIML_APP_SID");
        protected string _workspaceSid = Environment.GetEnvironmentVariable("TWILIO_ACME_WORKSPACE_SID");
        protected string _workflow_sid =Environment.GetEnvironmentVariable("TWILIO_ACME_WORKFLOW_SID");
        protected string _called_id = Environment.GetEnvironmentVariable("TWILIO_ACME_CALLERID");
                    
        class PolicyUrlUtils
        {
            const string taskRouterBaseUrl = "https://taskrouter.twilio.com";
            const string taskRouterVersion = "v1";

            readonly string _workspaceSid;
            readonly string _workerSid;

            public PolicyUrlUtils(string workspaceSid, string workerSid)
            {
                _workspaceSid = workspaceSid;
                _workerSid = workerSid;
            }

            public string AllTasks => $"{Workspace}/Tasks/**";
            public string Worker => $"{Workspace}/Workers/{_workerSid}";
            public string AllReservations => $"{Worker}/Reservations/**";
            public string Workspace =>
                $"{taskRouterBaseUrl}/{taskRouterVersion}/Workspaces/{_workspaceSid}";
            public string Activities => $"{Workspace}/Activities";

        }

        public IActionResult Index()
        {
         
            
            return View();
        }

        public IActionResult Incoming_call()
        {
            var action = new Uri("/Home/ProcessDigits");
            var response = new VoiceResponse();
            var gather = new Gather(timeout: 3, numDigits: 1, action: action);
            gather.Say("Welcome to ACME corp, please select your department");
            gather.Say("For Sales press one, for Support press two, for billing press three", language: "en-gb");

            response.Append(gather);


            return Content(response.ToString(), contentType: "text/xml");
        }

        public IActionResult ProcessDigits()

        {
            var response = new VoiceResponse();

            Dictionary<string, string> department = new Dictionary<string, string>();
            department.Add("1", "sales");
            department.Add("2", "support");
            department.Add("3", "billing");

            var enqueue = new Enqueue(workflowSid: _workflow_sid]);

            enqueue.Task("{'selected_product': '" + department[HttpContext.Request.Query["Digits"]] + @"'}");


            response.Append(enqueue);
            return Content(response.ToString(), contentType: "text/xml");

        }

        public IActionResult Agent_list()
        {
            TwilioClient.Init(_accountSid, _authToken);
            string expression = "worker.channel.voice.configured_capacity > 0";

            var agent_list = WorkerResource.Read(_workspaceSid, targetWorkersExpression: expression);
           

            foreach (var worker in agent_list)
            {
                Console.WriteLine("worker name: " + worker.FriendlyName);
            }
            ViewBag.voice_worker = agent_list;
            return View();

        }

        public IActionResult Chat()
        {

            return View();
        }


        [HttpPost]
        public IActionResult CallTransfer()
        {

            TwilioClient.Init(_accountSid, _authToken);


            string conferenceSid = HttpContext.Request.Query["conference"];
            string callSid = HttpContext.Request.Query["participant"];
            ParticipantResource.Update(
                conferenceSid,
                callSid,
                hold: true
            );

            try
            {

                string json = @"{
               'selected_product': 'manager',
              'conference': '" + HttpContext.Request.Query["conference"] + @"',
              'customer': '" + HttpContext.Request.Query["participant"]+ @"',
              'customer_taskSid': '" + HttpContext.Request.Query["taskSid"] + @"',
              'from': '" + HttpContext.Request.Query["from"] + @"',
               }";

                var task = TaskResource.Create(
                    _workspaceSid, attributes: JsonConvert.DeserializeObject(json).ToString(),
                    workflowSid: _workflow_sid
                );
               
            }
            catch (Exception e)
            {

                Console.Write(e.ToString());
            }

            return View();
        }

        public IActionResult CallMute()
        {

            TwilioClient.Init(_accountSid, _authToken);

            string conferenceSid = HttpContext.Request.Query["conference"];
            string callSid = HttpContext.Request.Query["participant"];
            bool muted = Convert.ToBoolean( HttpContext.Request.Query["muted"]);

            ParticipantResource.Update(
                conferenceSid,
                callSid,
                hold: muted
            );


            var response = new VoiceResponse();

            return Content(response.ToString(), contentType: "text/xml");
        }

        public IActionResult TransferTwiml()
        {

            var response = new VoiceResponse();
            var dial = new Dial();
            dial.Conference(HttpContext.Request.Query["conference"]);

            response.Append(dial);

            Console.Write(response.ToString());

            return Content(response.ToString(), contentType: "text/xml");
        }

        public IActionResult Agent_desktop()
        {

            string workerSid = HttpContext.Request.Query["WorkerSid"];
            TwilioClient.Init(_accountSid, _authToken);

            var activityDictionary = new Dictionary<string, string>();


            var activities = ActivityResource.Read(_workspaceSid);
            foreach (var activity in activities)
            {
                activityDictionary.Add(activity.FriendlyName, activity.Sid);

            }


            var updateActivityFilter = new Dictionary<string, Policy.FilterRequirement>
            {
                { "ActivitySid", Policy.FilterRequirement.Required }
            };

            var urls = new PolicyUrlUtils(_workspaceSid, workerSid);

            var allowActivityUpdates = new Policy(urls.Worker,
                HttpMethod.Post,
                postFilter: updateActivityFilter);
            var allowTasksUpdate = new Policy(urls.AllTasks, HttpMethod.Post);
            var allowReservationUpdate = new Policy(urls.AllReservations, HttpMethod.Post);
            var allowWorkerFetches = new Policy(urls.Worker, HttpMethod.Get);
            var allowTasksFetches = new Policy(urls.AllTasks, HttpMethod.Get);
            var allowReservationFetches = new Policy(urls.AllReservations, HttpMethod.Get);
            var allowActivityFetches = new Policy(urls.Activities, HttpMethod.Get);

            var policies = new List<Policy>
            {
                allowActivityUpdates,
                allowTasksUpdate,
                allowReservationUpdate,
                allowWorkerFetches,
                allowTasksFetches,
                allowReservationFetches

            };

            var capability = new TaskRouterCapability(
                _accountSid,
                _authToken,
                _workspaceSid,
                workerSid,
                policies: policies);

            var workerToken = capability.ToJwt();

            var scopes = new HashSet<IScope>
            {
                { new IncomingClientScope( HttpContext.Request.Query["WorkerSid"]) },
                { new OutgoingClientScope(_applicationSid) }
            };

            var webClientCapability = new ClientCapability(_accountSid, _authToken, scopes: scopes);
            var token = capability.ToJwt();

            ViewBag.worker_token = workerToken;
            ViewBag.client_token = webClientCapability.ToJwt();
            ViewBag.caller_ID = _called_id;
            ViewBag.activities = activityDictionary;
            return View();

        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
