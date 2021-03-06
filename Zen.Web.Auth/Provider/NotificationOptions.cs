﻿using System.Collections.Generic;

namespace Zen.Web.Auth.Provider {
    // https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerRegistration/showNotification
    public class NotificationOptions
    {
        public List<Action> actions = new List<Action>();
        public string body { get; set; }
        public string dir { get; set; } = "auto";
        public string lang { get; set; }
        public bool renotify { get; set; } = false;
        public bool requireInteraction { get; set; } = false;
        public string tag { get; set; }
        public List<int> vibrate { get; set; } = new List<int>();
        public Dictionary<string, string> data { get; set; } = new Dictionary<string, string>();
        public string icon { get; set; }
        public string image { get; set; }
        public string badge { get; set; }
        public class Action
        {
            public Action(string action, string title = null, string icon = null)
            {
                this.action = action;
                this.title = title ?? action;
                this.icon = icon;
            }

            public string action { get; set; }
            public string title { get; set; }
            public string icon { get; set; }
        }
    }
}