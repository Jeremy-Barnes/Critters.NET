import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { FormsModule, NgForm }   from '@angular/forms';
import { AuthResponse, User, SearchResult } from '../dto';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

    uo : Observable<User>;
    constructor(private userService: UserService) { 
        this.uo = this.userService.userSubject.asObservable();
    }

    searchQuery : string = "";
    searchResult! : SearchResult;
    user : User|null = null;

    signInClicked(loginForm: NgForm){
        this.userService.signIn(loginForm.value.emailAddress, loginForm.value.password);
    }

    usernameSearch(userName: string) {
        this.userService.searchUsers(userName)
        .subscribe(s => 
            this.searchResult = s
        );
    }

    ngOnInit(): void {
    }

}
