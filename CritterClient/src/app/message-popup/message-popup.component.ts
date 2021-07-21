import { Component, OnInit, Input } from '@angular/core';
import { ChatService } from '../chat.service';
import { ChannelDetails, Message, MessageDetails } from '../dto';

@Component({
  selector: 'message-popup',
  templateUrl: './message-popup.component.html',
  styleUrls: ['./message-popup.component.css']
})
export class MessagePopupComponent implements OnInit {

     @Input() channel: ActiveChannel;

    currentMessage: string = "";

    constructor(private chatService: ChatService) { }

    ngOnInit(): void {

    }

    changeMe(){
        this.channel.visible = false;
    }

    sendMessage(message: string){
        alert("Todo, send " + message + " to everyone in this channel!");
        this.currentMessage = "";
    }
}

export class ActiveChannel {
    constructor(channel: ChannelDetails) {
        this.channel = channel;
    }
    channel:ChannelDetails;
    visible: boolean = true;
}