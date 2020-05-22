import * as signalR from "@microsoft/signalr";


class Greeter {

    signalRConnection: signalR.HubConnection;

    constructor() {
    
        this.signalRConnection = new signalR.HubConnectionBuilder().withUrl("http://localhost:59010/notificationhub",
            {
                accessTokenFactory: () =>
                    "eyJhbGciOiJIUzM4NCIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImouYmFybmVzIiwiZW1haWwiOiJqZXJlbWlhaC5iYXJuZXNAb3V0bG9vay5jb20iLCJuYmYiOjE1ODg5OTk2MTIsImV4cCI6MTU5MDIwOTIxMiwiaWF0IjoxNTg4OTk5NjEyLCJpc3MiOiJjcml0dGVycyEifQ.w9yNPZtYLhxTOutWhpdG-Ou0p_ymvYKtxlDcpHDIY1ejHGJy7dLmnOnTmxpkXLIS"

            }).build();
        this.signalRConnection.on("ReceiveNotification", (notification: Notification) => { alert(notification.Message);});
        this.signalRConnection.start();
    }

    invokeSignalRServer(wo: number) {
        setInterval(() => this.signalRConnection.invoke("Connect"), 5000);
    }
}

class Notification {
    Message: string;
}

window.onload = () => {
    var greeter = new Greeter();
    greeter.invokeSignalRServer(33);
};