import { Component } from '@angular/core';
import { UserService } from "./user.service";
import { User } from "../types/User";

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent {
    title = 'critter-client';

    public user: User = new User();
    constructor(private userService: UserService) {
    }

    public logIn(userName: string, password: string) {
        this.userService.loginUser(userName, password)
        .subscribe(authResponse => this.user = authResponse.User);
    }

    onSubmit() {
        var self = this;
        this.logIn(this.user.UserName, this.user.Password);
    }

}


