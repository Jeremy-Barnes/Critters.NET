import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuthResponse, FriendshipDetails, SearchResult, User } from './dto';
import { environment } from './../environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, Observer, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';


@Injectable({
  providedIn: 'root'
})
export class UserService {

    public userSubject: BehaviorSubject<User> = new BehaviorSubject<User>(null);
    public friendSubject: BehaviorSubject<FriendshipDetails[]> = new BehaviorSubject<FriendshipDetails[]>(null);
    
    private jwtToken: string;

    constructor(private http: HttpClient) {
        this.jwtToken = '';
        this.cookieSignIn();
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
            this.userSubject.next(data.User);
            this.getUserFriends();
        });
    }

    cookieSignIn(): void {
        this.http.get<User>(environment.apiUrl + '/user/', { withCredentials : true })
        .pipe(
            retry(2),
            catchError(this.handleError),
        ).subscribe(user => {
            this.userSubject.next(user);
            this.getUserFriends();
        });
    }

    retrieveActiveUser() : Observable<User> {
        return this.http.get<User>(environment.apiUrl + '/user/',
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: this.jwtToken
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    getUserFriends(): void {
        this.retrieveFriends(this.jwtToken).subscribe(fds => {
            this.friendSubject.next(fds);
        });
    }

    retrieveFriends(jwt: string): Observable<FriendshipDetails[]> {
        return this.http.get<FriendshipDetails[]>(environment.apiUrl + '/user/friend',
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: 'Bearer ' + this.jwtToken
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    searchUsers(userSearchQuery: string) : Observable<SearchResult> {
        return this.http.get<SearchResult>(environment.apiUrl + '/search/' + userSearchQuery,
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: 'Bearer ' + this.jwtToken
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        )
    }

    getUser(userName: string) : Observable<User> {
        return this.http.get<User>(environment.apiUrl + '/user/' + userName,
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: 'Bearer ' + this.jwtToken
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    addFriend(friendUserName: string) : Observable<FriendshipDetails> {
        return this.http.put<FriendshipDetails>(environment.apiUrl + '/user/friend/' + friendUserName, null,
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: 'Bearer ' + this.jwtToken
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    removeFriend(friendUserName: string) : Observable<FriendshipDetails> {
        return this.http.delete<FriendshipDetails>(environment.apiUrl + '/user/friend/' + friendUserName,
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: 'Bearer ' + this.jwtToken
              })
        })
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
        return throwError(
          'Something bad happened; please try again later.');
    }
}
