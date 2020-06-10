import * as signalR from "@microsoft/signalr";
import * as jQuery from 'jquery';

class Greeter {

    signalRConnection: signalR.HubConnection;

    constructor() {
        //this.signalRConnection = new signalR.HubConnectionBuilder().withUrl("http://localhost:59010/notificationhub",
        //    {
        //        accessTokenFactory: () =>
        //            "eyJhbGciOiJIUzM4NCIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImMuZGlwc29uIiwiZW1haWwiOiJqYWJhcm5lczIxMTJAZ21haWwuY29tIiwibmJmIjoxNTkwMTY2MjkwLCJleHAiOjE1OTEzNzU4OTAsImlhdCI6MTU5MDE2NjI5MCwiaXNzIjoiY3JpdHRlcnMhIn0.hyM_bGStaTpeayOV8CetLi9oCguTZ6BSaX99-T02Ja0OZA4mE7FlYlUR8leHLScz"

        //    }).build();
        //this.signalRConnection.on("ReceiveNotification", (notification: Notification) => { alert(notification.AlertText);});
        //this.signalRConnection.start();
    }

    //invokeSignalRServer(wo: number) {
    //    setInterval(() => this.signalRConnection.invoke("Connect"), 1000*25);
    //}

    SignUp = () => {
        var userName = (<any>document.getElementById('sign_username')).value;
        var email = (<any>document.getElementById('sign_email')).value;
        var password = (<any>document.getElementById('sign_password')).value;
    }

    Bet = () => {
        var _this = this;
        var betOn = <number>(<any>document.getElementById('beton')).value;
        var wager = <number>(<any>document.getElementById('betamt')).value;

        var output = Greeter.doAjax("command/" + Greeter.gameID, "game", JSON.stringify({
            CashWagered: wager,
            NumberGuessed: betOn,
        }), "PUT");
    }


    LogIn = () => {
        var _this = this;
        var userName = (<any>document.getElementById('username')).value;
        var password = (<any>document.getElementById('password')).value;
        Greeter.gameID = (<any>document.getElementById('gameid')).value;

        var output = Greeter.doAjax("login", "user", {
            UserName: userName,
            Password: password,
        }).done((s:any)=> {
            if (s.authToken) {
                Greeter.token = s.authToken;
                var cash = document.getElementById('CashOnHand').innerText += ("Cash to bet with:" + s.user.Cash);

                _this.signalRConnection = new signalR.HubConnectionBuilder().withUrl("http://localhost:59010/gamehub",
                    {
                        accessTokenFactory: () =>
                            Greeter.token
                    }).build();
                _this.signalRConnection.on("ReceiveNotification", (serverNotification: GameAlert) => { alert(serverNotification.AlertText); });
                _this.signalRConnection.on("ReceiveSystemMessage", (message: string) => { document.getElementById('chatZone').innerHTML += "<br /> " + message });
                _this.signalRConnection.on("ReceiveChat", (sender: string, message: string) => { document.getElementById('chatZone').innerHTML += "<br /> <b>" + sender + "</b>: " + message });

                _this.signalRConnection.start().then(() => {
                    _this.signalRConnection.invoke("Connect", Greeter.gameID);
                })
                document.getElementById('authzone').remove();
            } else {
                document.getElementById('authzone').innerHTML = ("<h3>Bad Login Try Again</h3>" + document.getElementById('authzone').innerHTML)
            }
        });
    }

    Chat= () => {
        var text = (<any>document.getElementById('chatbox')).value;
        document.getElementById('chatZone').innerHTML += "<br /> <b>You</b>: " + text;
        this.signalRConnection.invoke("SendChatMessage", text, Greeter.gameID);
    }

    static token: string;
    static gameID: string;

    static baseURL: string = "http://" + "localhost:59010" + "/api/";

    private static doAjax(functionName: string, functionService: string, parameters: any, type: string = "POST"): JQueryPromise<any> {
        var param = parameters != null && parameters.constructor === String ? parameters : JSON.stringify(parameters);
        var pathParams = type == "GET" && parameters != null ? "/" + param : "";
        var settings: JQueryAjaxSettings = {
            url: Greeter.baseURL + functionService + "/" + functionName + pathParams,
            method: type,
            contentType: "application/json",
            xhrFields: {
                withCredentials: true,
            },
            cache: false,
            headers: {
                Authorization: Greeter.token,
            },
            success: (json, status, args) => {
                //alert(args);
            },
            data: type == "POST" ? param : "",
            crossDomain: true,
        };
        return jQuery.ajax(settings);
    }
}

class GameAlert {
    AlertText: string;
    GameType: number;
}


window.onload = () => {
    var greeter = new Greeter();
    document.getElementById('loginbutt').addEventListener('click', greeter.LogIn);
    document.getElementById('createbutt').addEventListener('click', greeter.SignUp);
    document.getElementById('chatbutt').addEventListener('click', greeter.Chat);
    document.getElementById('betbutt').addEventListener('click', greeter.Bet);





};