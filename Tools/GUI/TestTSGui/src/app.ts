import * as signalR from "@microsoft/signalr";


class Greeter {

    signalRConnection: signalR.HubConnection;

    constructor() {
    
        this.signalRConnection = new signalR.HubConnectionBuilder().withUrl("http://localhost:59010/notificationhub",
            {
                accessTokenFactory: () =>
                    "eyJhbGciOiJIUzM4NCIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImMuZGlwc29uIiwiZW1haWwiOiJqYWJhcm5lczIxMTJAZ21haWwuY29tIiwibmJmIjoxNTkwMTY2MjkwLCJleHAiOjE1OTEzNzU4OTAsImlhdCI6MTU5MDE2NjI5MCwiaXNzIjoiY3JpdHRlcnMhIn0.hyM_bGStaTpeayOV8CetLi9oCguTZ6BSaX99-T02Ja0OZA4mE7FlYlUR8leHLScz"

            }).build();
        this.signalRConnection.on("ReceiveNotification", (notification: Notification) => { alert(notification.AlertText);});
        this.signalRConnection.start();
    }

    invokeSignalRServer(wo: number) {
        setInterval(() => this.signalRConnection.invoke("Connect"), 1000*25);
    }
}

class Notification {
    AlertText: string;
}


window.onload = () => {
    var greeter = new Greeter();
    greeter.invokeSignalRServer(33);
};