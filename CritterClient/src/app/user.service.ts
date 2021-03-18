import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuthResponse, User } from './dto';
import { environment } from './../environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';


@Injectable({
  providedIn: 'root'
})
export class UserService {

    private user: User;
    private userObs!: Observable<User>;
    private signInData: AuthResponse;
    constructor(private http: HttpClient) { 
        this.user = new User();
        this.signInData = new AuthResponse();
        //todo some cookie bullshit here
    }

    signIn(userNameOrEmail: string, password: string) : Observable<User> {
        let email = null;
        let userName = null;
        if(userNameOrEmail.includes('@')) {
            email = userNameOrEmail;
        } else {
            userName = userNameOrEmail;
        }
        this.http.post<AuthResponse>(environment.apiUrl + "/user/login/", 
        {
            UserName: userName,
            FirstName: "",
            LastName: "",
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
            this.signInData = data;
            this.fullSignIn(this.signInData.AuthToken)
        });
        //this.userObs = this.cookieSignIn();
        this.userObs.subscribe();
        return this.userObs;
    }

    cookieSignIn() : Observable<User> {
        return this.http.get<User>(environment.apiUrl + "/user/", {withCredentials : true })
        .pipe(
            retry(2),
            catchError(this.handleError),
        );
    }

    fullSignIn(jwt: string) : Observable<User> {
        return this.http.get<User>(environment.apiUrl + "/user/",
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
