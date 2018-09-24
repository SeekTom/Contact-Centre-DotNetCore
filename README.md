# Contact-Centre Version C# 0.1

Inbound PSTN to Twilio Client Contact Centre Powered by Taskrouter 

Languages: C#, js

This implements:

-  Single channel (Voice)
- 4 departments
- Agent UI based on TaskRouter SDK for low latency
- Twilio Client WebRTC dashboard
- Conference instruction
- Call instruction
- Conference recording
- Call holding
- Call transfers
- optional assignment_callback url implementation

This has been created in VS 2017 and so the newer Csproj file will not work in older versions, if you cannot upgrade to 2017 or later, you can open this project in VS Code:

https://code.visualstudio.com/

## Setup
1. Setup a new TwiML App https://www.twilio.com/console/voice/twiml/apps and point it to the domain where you deployed this app (add `/incoming_call` suffix): `https://YOUR_DOMAIN_HERE/incoming_call`
2. Buy a Twilio number https://www.twilio.com/console/phone-numbers/incoming
3. Configure your number to point towards http://yourdomain.com/home/incoming_call
4. Define the following in the home controller:

```
protected string _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACME_ACCOUNT_SID");
protected string  _authToken = Environment.GetEnvironmentVariable("TWILIO_ACME_AUTH_TOKEN");
protected string _applicationSid =  Environment.GetEnvironmentVariable("TWILIO_ACME_TWIML_APP_SID");
protected string _workspaceSid = Environment.GetEnvironmentVariable("TWILIO_ACME_WORKSPACE_SID");
protected string _workflow_sid =Environment.GetEnvironmentVariable("TWILIO_ACME_WORKFLOW_SID");
protected string _called_id = Environment.GetEnvironmentVariable("TWILIO_ACME_CALLERID");
```
This is not production code and is for information purposes only
