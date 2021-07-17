import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuthResponse, FriendshipDetails, User } from './dto';
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

    signIn(userNameOrEmail: string, password: string) {
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
            FirstName: '',
            LastName: '',
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
        ).subscribe((data : AuthResponse) => {
            this.jwtToken = data.AuthToken;
            this.userSubject.next(data.User);
        });
    }

    cookieSignIn() {
        this.http.get<User>(environment.apiUrl + '/user/', { withCredentials : true })
        .pipe(
            retry(2),
            catchError(this.handleError),
        ).subscribe(o => this.userSubject.next(o));
    }

    getUserFriends(){
        this.retrieveFriends(this.jwtToken).subscribe(fds => {
            this.friendSubject.next(fds);
        });
    }

    retrieveUser(jwt: string): Observable<User> {
        return this.http.get<User>(environment.apiUrl + '/user/',
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: jwt
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    retrieveFriends(jwt: string): Observable<FriendshipDetails[]> {
        return this.http.get<FriendshipDetails[]>(environment.apiUrl + '/user/friend',
        {
            withCredentials : true,
            headers: new HttpHeaders({
                'Content-Type':  'application/json',
                Authorization: jwt
              })
        })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    private handleError(error: HttpErrorResponse) {
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
