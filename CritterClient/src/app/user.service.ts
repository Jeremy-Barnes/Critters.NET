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
    constructor(private http: HttpClient) { 

        this.user = new User();
        //todo some cookie bullshit here
    }

    signIn(userNameOrEmail: string, password: string) : Observable<AuthResponse> {
        return this.http.post<AuthResponse>(environment.apiUrl + "/user/login/", 
        {
            UserName: "",
            FirstName: "",
            LastName: "",
            EmailAddress: "",
            Password: ""
        }, 
        {
            headers: new HttpHeaders(
                {
                    'Content-Type':  'application/json'
                })
        }).pipe(
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
