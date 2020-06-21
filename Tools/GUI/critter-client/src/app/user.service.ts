import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { User } from "../types/User";
import { catchError, map } from 'rxjs/operators';
import { stringify } from 'querystring';

@Injectable({
    providedIn: 'root'
})
export class UserService {

    private apiUrl = 'http://localhost:59010/api/user/';

    constructor(private http: HttpClient) {
    }

    public loginUser(userName: string, password: string) {
        return this.http.post<{AuthToken: string; User: User}>(this.apiUrl + "login", { UserName: userName, Password: password });
    }
}
