import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { FormsModule, NgForm }   from '@angular/forms';
import { AuthResponse, User } from '../dto';
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

    signInClicked(loginForm: NgForm){
        this.userService.signIn(loginForm.value.emailAddress, loginForm.value.password);
    }

    ngOnInit(): void {
    }

}
