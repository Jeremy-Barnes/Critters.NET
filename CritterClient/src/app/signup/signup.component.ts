import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute, ParamMap } from '@angular/router';
import { User } from '../dto';
import { UserService } from '../user.service';
import { BasicInfoComponent } from './basic-info/basic-info.component';
import { UserDetailsComponent } from './user-details/user-details.component';

@Component({
    selector: 'app-signup',
    templateUrl: './signup.component.html',
    styleUrls: ['./signup.component.css']
})
export class SignupComponent implements OnInit {

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private userService: UserService) {
            this.userService.userSubject.next(new User());
    }

    ngOnInit(): void {
        const link = ['0'];
        this.router.navigate(link, { relativeTo: this.route });
    }
}
