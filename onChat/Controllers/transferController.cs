using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using onChat.Models;
using onChat.Hubs;

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Messaging;
//using FirebaseAdmin.messaging;

namespace onChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class transferController : Controller
    {
        private IHubContext<MyHub> hub;
        public static int indexId = 10;
        private Service service;
        List<User> usersList = new List<User>();
        public static Dictionary<string, string> myMap = new Dictionary<string, string>();
        public transferController(IHubContext<MyHub> hub)
        {
            service = new Service();
            usersList = service.GetAll();
            this.hub = hub;
            myMap = service.GetDic();
        }

        [HttpPost()]
        public IActionResult Transfer([Bind(" from, to, content")] AddContactTransfer newContact)
        {
            Models.Message message = new Models.Message();
            message.content = newContact.content;
            message.id = indexId.ToString();
            indexId++;
            DateTime time = DateTime.Now;
            string format = "HH:mm";
            message.created = time.ToString(format);
            message.sent = false;


            if (service.GetDic().ContainsKey(newContact.to))
                {
                    var registrationToken = getToken(newContact.to);
                var messsage = new FirebaseAdmin.Messaging.Message()
                {
                    Data = new Dictionary<string, string>()
                        {
                            {"myData","1337" },
                        },
                    Token = registrationToken,
                    Notification = new Notification()
                    {
                        Title = "from:"+newContact.from+":to:"+newContact.to,
                        Body = newContact.content
                    }
                };
                string response = FirebaseMessaging.DefaultInstance.SendAsync(messsage).Result;
                
                }
          
        


            for (int i = 0; i < usersList.Count; i++)
            {
                if (usersList[i].id == newContact.to)
                {
                    for (int j = 0; j < usersList[i].chats.Count; j++)
                    {
                        for (int k = 0; k < usersList[i].contacts.Count; k++)
                        {
                            if (usersList[i].contacts[k].id == newContact.from)
                            {
                                usersList[i].contacts[k].last = message.content;
                                usersList[i].contacts[k].lastdate = message.created;
                            }

                        }
                        if (usersList[i].chats[j].username == newContact.from)
                        {
                            usersList[i].chats[j].messages.Add(message);
                            hub.Clients.All.SendAsync("ReceiveMessage", newContact.to, newContact.from);
                            return StatusCode(201);
                        }
                    }
                }
            }
            return NotFound();
        }

        

        [HttpGet("{id}")]
        public string getToken(string id)
        {
            return myMap[id];
        }

        [HttpPost("{id}/{token}")]
        public void setToken(string id, string token)
        {
            if (myMap.ContainsKey(id))
            {
                return;
            }
            myMap.Add(id, token);
        }

    }
}