import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuthResponse, FriendshipDetails, SearchResult, User } from './dto';
import { environment } from './../environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, Observer, throwError } from 'rxjs';
import { catchError, retry, tap } from 'rxjs/operators';


@Injectable({
  providedIn: 'root'
})
export class UserService {

    public activeUserSubject: BehaviorSubject<User> = new BehaviorSubject<User>(null);
    public friendsListSubject: BehaviorSubject<FriendshipDetails[]> = new BehaviorSubject<FriendshipDetails[]>(null);
    
    private jwtToken: string;

    constructor(private http: HttpClient) {
        this.jwtToken = '';
        this.initializeActiveUser();    
    }

    signIn(userNameOrEmail: string, password: string): void {
        let email = null;
        let userName = null;
        if (userNameOrEmail.includes('@')) {
            email = userNameOrEmail;
        } else {
            userName = userNameOrEmail;
        }
        this.http.post<AuthResponse>(environment.apiUrl + '/user/login/',
        {
            UserName: userName,
            EmailAddress: email,
            Password: password
        },
        {
            withCredentials : true,
            headers: new HttpHeaders(
            {
                'Content-Type':  'application/json'
            })
        }).pipe(
            retry(2),
            catchError(this.handleError),
        ).subscribe((data: AuthResponse) => {
            this.jwtToken = data.AuthToken;
            this.activeUserSubject.next(data.User);
            this.initializeActiveUserFriends();
        });
    }

    private initializeActiveUser() : void {
        this.retrieveUser(null)
        .pipe(tap(this.initializeActiveUserFriends))
        .subscribe(user => {
            this.activeUserSubject.next(user);
            // this.initializeActiveUserFriends();
        })
    }

    private initializeActiveUserFriends(): void {
        this.retrieveFriends().subscribe(fds => {
            this.friendsListSubject.next(fds);
        });
    }

    retrieveUser(userName: string) : Observable<User> {
        return this.http.get<User>(environment.apiUrl + `/user/${userName ? userName : ''}`, this.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    retrieveFriends(): Observable<FriendshipDetails[]> {
        return this.http.get<FriendshipDetails[]>(environment.apiUrl + '/user/friend', this.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    searchUsers(userSearchQuery: string) : Observable<SearchResult> {
        return this.http.get<SearchResult>(environment.apiUrl + '/search/' + userSearchQuery, this.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        )
    }

    addFriend(friendUserName: string) : Observable<FriendshipDetails> {
        return this.http.put<FriendshipDetails>(environment.apiUrl + '/user/friend/' + friendUserName, null, this.httpOptionsAuthJson())
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    removeFriend(friendUserName: string) : Observable<FriendshipDetails> {
        return this.http.delete<FriendshipDetails>(environment.apiUrl + '/user/friend/' + friendUserName,
        this.httpOptionsAuthJson()
        )
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    private httpOptionsAuthJson(){
        return {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: 'Bearer ' + this.jwtToken
              })
        };
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
        return throwError(
          'Something bad happened; please try again later.');
    }
}
