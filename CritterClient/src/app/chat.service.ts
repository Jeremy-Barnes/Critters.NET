import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { ChannelDetails, MessageDetails, MessageResponse, Message, Channel, User } from './dto';
import { UserService } from './user.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {

    constructor(private http: HttpClient, private userService: UserService) {
        userService.activeUserSubject.subscribe(u => { 
            if(u && u.UserName && userService.isAuthenticated()) { this.getUnreadMessagesPage(true); this.getMessagesPage(true);
        }});
    }

    public UnreadMessages: BehaviorSubject<MessageDetails[]> = new BehaviorSubject<MessageDetails[]>(null);
    public Channels: BehaviorSubject<ChannelDetails[]> = new BehaviorSubject<ChannelDetails[]>(null);

    private lastNewMessageId: number;
    private lastMessageId: number;

    getUnreadMessagesPage(onlyNewest: boolean){
        this.retrieveUnreadMessages(onlyNewest? null : this.lastNewMessageId).subscribe(mr => {
            var chans = this.Channels.getValue();
            mr.ChannelDetails.forEach(cd => {
                this.UpdateUnreadMessages(cd);
                this.UpdateChannelInfo(cd, chans ?? []);
            });
        });
    }

    getMessagesPage(onlyNewest: boolean){
        this.retrieveMessages(onlyNewest? null : this.lastMessageId).subscribe(mr => {
            var chans = this.Channels.getValue();
            mr.ChannelDetails.forEach(cd => {
                //this.UpdateInbox(cd);
                this.UpdateChannelInfo(cd, chans ?? []);
            });
        });
    }

    getMessagesBatch(channelId: number){
        var channel = this.Channels?.getValue().filter(c => c.Channel.ChannelId == channelId);
        if(channel == null) {
            //fetch this channel and its first messages!!!!!!!!
        }
        var lastId = -1;
        if(channel[0].Messages.length == 0) {
            return;
        }
        lastId = channel[0].Messages[channel[0].Messages.length -1].Message.MessageId;
        this.retrieveMessagesFromParent(lastId).subscribe(mr => {
            var chans = this.Channels.getValue();
            mr.ChannelDetails.forEach(cd => {
                this.UpdateChannelInfo(cd, chans ?? []);
            });
        });
    }

    private UpdateUnreadMessages(cd: ChannelDetails) {
        if(cd.Messages.length == 0) return;

        if(!this.lastNewMessageId) 
            this.lastNewMessageId = cd.Messages[cd.Messages.length - 1].Message.MessageId;
        else {
            if(this.lastNewMessageId > cd.Messages[cd.Messages.length - 1].Message.MessageId){
                this.lastNewMessageId = cd.Messages[cd.Messages.length - 1].Message.MessageId;
            }
        }
        if(!this.lastMessageId) 
            this.lastMessageId = cd.Messages[cd.Messages.length - 1].Message.MessageId;
        else {
            if(this.lastMessageId > cd.Messages[cd.Messages.length - 1].Message.MessageId){
                this.lastMessageId = cd.Messages[cd.Messages.length - 1].Message.MessageId;
            }
        }
        var inboxVal = this.UnreadMessages.getValue();
        if(inboxVal == null) {
            inboxVal = cd.Messages;
        } else {
            var newMessages = cd.Messages.filter(m => inboxVal.findIndex(ibx => ibx.Message.MessageId == m.Message.MessageId) == -1);
            inboxVal = inboxVal.concat(newMessages);
        }
        this.UnreadMessages.next(inboxVal);
    }

    private UpdateChannelInfo(newChannelDet: ChannelDetails, allChannels: ChannelDetails[]) {
        var currentChan = allChannels?.find(ch => ch.Channel.ChannelId == newChannelDet.Channel.ChannelId);
        if(!currentChan){
            allChannels.push(newChannelDet);
        } else {
            currentChan.Channel = newChannelDet.Channel;
            currentChan.UserNames = newChannelDet.UserNames;
            currentChan.Users = newChannelDet.Users;

            if(currentChan.Messages == null) {
                currentChan.Messages = newChannelDet.Messages;
            } else {
                currentChan.Messages = currentChan.Messages.concat(newChannelDet.Messages
                    .filter(m => currentChan.Messages
                        .findIndex(cChan => cChan.Message.MessageId == m.Message.MessageId) == -1));
            }
            
        }
        this.Channels.next(allChannels);
    }

    retrieveUnreadMessages(lastId: number | null){
        return this.http.get<MessageResponse>(environment.apiUrl + `/message/new/${lastId ? lastId : ''}`, this.userService.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    retrieveMessages(lastId: number | null){
        return this.http.get<MessageResponse>(environment.apiUrl + `/message/${lastId ? 'page/' + lastId : ''}`, this.userService.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    retrieveMessagesFromParent(parentId: number){
        return this.http.get<MessageResponse>(environment.apiUrl + `/message/thread/${parentId}`, this.userService.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    private handleError(error: HttpErrorResponse): Observable<never> {
        if (error.error instanceof ErrorEvent) {
            // A client-side or network error occurred. Handle it accordingly.
            console.error('An error occurred:', error.error.message);
        } else {
            // The backend returned an unsuccessful response code.
            // The response body may contain clues as to what went wrong.
            console.error(
                `Backend returned code ${error.status}, ` +
                `body was: ${error.error}`);
        }
        // Return an observable with a user-facing error message.
        return throwError('Something bad happened; please try again later.');
    }
}
